# Sprint Change Proposal: CQRS Hard Rule — Aggregates Are Write-Only

**Date:** 2026-03-15
**Author:** John (PM Agent)
**Requested by:** Jerome
**Scope:** Minor — Document edits only
**Sprint Impact:** None — current story (2-2) proceeds unchanged

---

## Section 1: Issue Summary

**Problem Statement:** The project artifacts (PRD, Architecture, Epics) describe query functionality without explicitly mandating that all queries are served by projections that subscribe to domain events via DAPR pub/sub. Aggregates are write-only — this is a fundamental CQRS principle in the Hexalith.EventStore ecosystem — but the documents leave the query data source ambiguous. FRs say "a developer can query..." without stating "from a projection read model." The Architecture describes the projection pattern correctly but frames it as a design decision rather than a hard constraint.

**Discovery Context:** Identified during Sprint 1, Epic 2 implementation. No code is affected (all completed work is write-side), but the ambiguity would cause problems when Epic 5 (projections/queries) implementation begins.

**Evidence:**

- FRs 25-30 are source-agnostic on query mechanism
- FR31 conflates query behavior (read-side) with role enforcement (write-side) under a single Epic 3 mapping
- Architecture document lacks a single, explicit "aggregates are write-only" constraint
- Architecture D4 frames projections as a "decision" rather than the only valid path

---

## Section 2: Impact Analysis

### Epic Impact

| Epic | Impact | Details |
|------|--------|---------|
| Epic 1 | None | Pure scaffolding — no query references |
| Epic 2 | None | Write-side aggregates and CommandApi — unaffected |
| Epic 3 | Minor | FR31 query half moves to Epic 5 coverage. Write-side stories unchanged |
| Epic 4 | None | Already implements the correct pattern (consuming services subscribe via pub/sub) |
| Epic 5 | Moderate | Wording tightened in Stories 5.1, 5.2, 5.3. FRs 25-30 updated. FR31 query coverage added |
| Epic 6 | Minor | Stories 6.1, 6.2 — clarify aggregate vs projection isolation testing language |
| Epic 7 | Minor | Story 7.2 — add "via pub/sub" to projection event processing AC |
| Epic 8 | None | Documentation stories unaffected |

### Story Impact

| Story | Status | Change Needed |
|-------|--------|---------------|
| 2-1 | done | None |
| 2-2 | ready-for-dev | None — proceed as-is |
| 2-3, 2-4 | backlog | None |
| 3-1, 3-2, 3-3 | backlog | 3.2 keeps write-side FR31 only; query authorization noted as Epic 5 concern |
| 5-1 | backlog | ACs: add "via pub/sub event subscription" language |
| 5-2 | backlog | ACs: clarify query source is projection state store |
| 5-3 | backlog | ACs: add FR31b query authorization coverage |
| 6-1 | backlog | AC: fix "aggregate-level isolation guarantee" terminology for projection context |
| 6-2 | backlog | AC: clarify conformance scope (aggregate parity, not subscription parity) |
| 7-2 | backlog | AC: add "via DAPR pub/sub" to event processing description |

### Artifact Conflicts

| Artifact | Severity | Changes Needed |
|----------|----------|----------------|
| Architecture | High | Add hard rule as top-level principle. Reframe D4, strengthen D7, add decoupling note |
| PRD | Medium | FRs 25-30 add "from projection read models." FR31 split. Line 178 clarify query source |
| Epics | Medium | Wording updates in 7 stories across Epics 3, 5, 6, 7 |

### Technical Impact

Zero code changes. All completed work (Stories 1.1-2.1) is write-side only. Story 2-2 (ready-for-dev) is unaffected.

---

## Section 3: Recommended Approach

**Selected Path:** Direct Adjustment (Option 1)

**Approach:** Update three artifacts (Architecture, PRD, Epics) with explicit hard rule language. No code changes, no epic restructuring, no rollback.

**Rationale:**

- The correct architecture is already described — we're eliminating ambiguity, not redesigning
- All completed work is write-side only and fully compliant with the hard rule
- Current sprint story (2-2) is unaffected and can proceed immediately
- The fix prevents future misimplementation when Epic 5 (projections/queries) begins
- Cost is minimal (document edits only) with high preventive value

**Effort estimate:** Low — 11 targeted edits across 3 documents

**Risk level:** Low — no code changes, no behavioral changes, no timeline impact

**Timeline impact:** None — Sprint 1 execution continues uninterrupted

**Alternatives considered:**

- Defer to Epic 5 start: Rejected — ambiguity now risks incorrect assumptions in Stories 3.1-3.3 (backlog) which reference role behavior that borders read/write boundary
- Full PRD rewrite: Rejected — overkill. Targeted FR edits achieve the same constraint enforcement

---

## Section 4: Detailed Change Proposals

### Architecture Edits (5 edits)

**Edit 1 — Add Hard Rule as Top-Level Principle**

Section: Architectural Priorities (after existing item 4)

Insert new priority:

> 5. **CQRS Hard Rule: Aggregates are write-only** — Aggregates handle commands and produce events. All read/query operations are served exclusively by projections that subscribe to domain events via DAPR pub/sub and maintain read models in DAPR state stores. No component may query aggregate state directly. Aggregate state is used exclusively during command processing (Handle methods) for write-side validation. This is non-negotiable.

**Edit 2 — Reframe D4 from "decision" to "constraint"**

Section: Read Model (Query Side) — D4

OLD:
> **Read Model (Query Side):**
> - Decision: `EventStoreProjection<TReadModel>` pattern with DAPR state store
> - Rationale: EventStore provides `EventStoreProjection<TReadModel>` base class with reflection-based Apply method discovery. Assembly scanning auto-discovers projections. DAPR pub/sub routes events to projections. Read model stored in convention-named DAPR state store

NEW:
> **Read Model (Query Side):**
> - Constraint: All queries are served by `EventStoreProjection<TReadModel>` projections with DAPR state store. Aggregates are write-only — no query path may read aggregate state.
> - Mechanism: EventStore provides `EventStoreProjection<TReadModel>` base class with reflection-based Apply method discovery. Assembly scanning auto-discovers projections. Projections subscribe to domain events via DAPR pub/sub and maintain read models in convention-named DAPR state stores. Query endpoints read exclusively from these state stores.

**Edit 3 — D7 add explicit query-aggregate boundary**

Section: D7 Revision — Dual-Layer Query Architecture

ADD after the External bullet:

> - Boundary: Query dispatch terminates at the projection layer. It NEVER reaches aggregates. The full data path is: Domain events → DAPR pub/sub → Projections → DAPR state store → Query endpoints read from state store

**Edit 4 — Package structure decoupling note**

Section: Package Structure

OLD:
> - Server contains: Aggregates + Projections (both auto-discovered via assembly scanning)

NEW:
> - Server contains: Aggregates + Projections (both auto-discovered via assembly scanning). Co-located for deployment simplicity but decoupled at runtime — projections receive events exclusively via DAPR pub/sub subscriptions, never by reading aggregate state

**Edit 5 — Reframe architectural priority #4**

Section: Architectural Priorities, item 4

OLD:
> 4. **Read model architecture** — The tenant service needs its own query endpoints (FR25-30) — this goes beyond the pure command-side that EventStore domain services typically handle

NEW:
> 4. **Read model architecture** — The tenant service implements the standard CQRS read side via projections that subscribe to domain events and maintain read models in DAPR state stores. Query endpoints (FR25-30) read exclusively from these projection state stores, following the ecosystem-wide pattern where aggregates are write-only

### PRD Edits (3 edits)

**Edit 6 — FRs 25-30 add projection qualifier**

OLD:
> - FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses
> - FR26: A developer can query a specific tenant's details including its current users and their roles
> - FR27: A developer can query the list of users in a specific tenant with their assigned roles
> - FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant
> - FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000)
> - FR30: All list and query endpoints support cursor-based pagination with consistent ordering

NEW:
> - FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses, served from projection read models
> - FR26: A developer can query a specific tenant's details including its current users and their roles, served from projection read models
> - FR27: A developer can query the list of users in a specific tenant with their assigned roles, served from projection read models
> - FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant, served from projection read models
> - FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, served from projection read models, with pagination support (default page size: 100 results, maximum: 1,000)
> - FR30: All list and query endpoints support cursor-based pagination with consistent ordering, reading from projection state stores

**Edit 7 — FR31 split**

OLD:
> - FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands

NEW:
> - FR31: A TenantReader cannot execute any state-changing commands — the aggregate rejects commands from users with TenantReader role
> - FR31b: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, served from projection read models with role-based authorization at the query layer

FR Coverage Map update:

OLD:
> FR31: Epic 3 - TenantReader query-only behavior

NEW:
> FR31: Epic 3 - TenantReader command rejection (write-side)
> FR31b: Epic 5 - TenantReader query authorization (read-side, projection-based)

**Edit 8 — Additional Requirements line 178 clarify query source**

OLD:
> - Query endpoints served from CommandApi as route groups (single deployable) -- `POST /api/commands` and `GET /api/tenants/*`

NEW:
> - Query endpoints served from CommandApi as route groups (single deployable) -- `POST /api/commands` and `GET /api/tenants/*`. Query endpoints read from projection state stores, never from aggregate state

### Epics Edits (3 edits)

**Edit 9 — Stories 5.1, 5.2, 5.3 tighten projection language**

Story 5.1 — replace last AC:

OLD:
> **Then** projections are auto-discovered via EventStore's assembly scanning and registered for event processing

NEW:
> **Then** projections are auto-discovered via EventStore's assembly scanning, registered as DAPR pub/sub event subscribers, and maintain read models in DAPR state stores. Query endpoints read exclusively from these state stores, never from aggregate state

Story 5.2 — replace AC:

OLD:
> **When** the index is queried

NEW:
> **When** a query endpoint reads from the index projection state store

Story 5.3 — add new AC:

> **Given** an authenticated user with TenantReader role in the target tenant
> **When** a GET request is sent to `/api/tenants/{tenantId}` or `/api/tenants/{tenantId}/users`
> **Then** the query succeeds, returning data from projection read models (FR31b)

**Edit 10 — Stories 6.1 and 6.2 clarify isolation terminology**

Story 6.1 — replace AC:

OLD:
> **Then** projections for tenant A never contain data from tenant B (aggregate-level isolation guarantee)

NEW:
> **Then** aggregate state for tenant A never contains data from tenant B (aggregate-level isolation guarantee). Projection-level isolation is a separate concern tested in Story 6.2

Story 6.2 — replace first two ACs:

OLD:
> **Then** the projection maintains queryable tenant state (tenant details, user lists, configuration) in memory

NEW:
> **Then** the projection maintains queryable tenant state (tenant details, user lists, configuration) in memory. Note: in tests, events are applied directly to the projection as a testing shortcut — in production, projections receive events via DAPR pub/sub subscription

OLD:
> **Then** results are returned from the in-memory projection without DAPR state store dependency

NEW:
> **Then** results are returned from the in-memory projection (substituting for the DAPR state store used in production). All queries read from the projection, never from aggregate state

**Edit 11 — Story 7.2 add pub/sub to projection AC**

OLD:
> **Given** the tenant service is processing events for projections

NEW:
> **Given** the tenant service is processing events for projections via DAPR pub/sub subscriptions

---

## Section 5: Implementation Handoff

**Change Scope:** Minor — Document edits only, no code changes, no epic restructuring.

**Responsibilities:**

| Role | Action |
|------|--------|
| Architect / Dev | Apply 5 edits to architecture.md |
| PM / Dev | Apply 3 edits to prd.md (FRs 25-30, FR31 split, line 178) |
| SM / Dev | Apply 3 edits to epics.md (Stories 5.1/5.2/5.3, 6.1/6.2, 7.2) |

**Success Criteria:**

- All 11 edits applied to the 3 artifacts
- The hard rule is stated as a top-level architectural principle in architecture.md
- No FR, story, or AC implies querying from aggregate state
- Story 2-2 proceeds unchanged
- Future story creation (create-story workflow) respects the hard rule

**Next Steps:**

1. Apply the 11 document edits
2. Continue Sprint 1 — implement Story 2-2 (GlobalAdministratorsAggregate) as planned
3. When Stories 3.1-3.3 specs are created, verify they reference the hard rule for write-side role enforcement
4. When Epic 5 stories are created, verify all projection ACs reference pub/sub subscription and state store reads
