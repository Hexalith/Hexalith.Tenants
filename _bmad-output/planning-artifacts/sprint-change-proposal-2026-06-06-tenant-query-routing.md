---
title: "Sprint Change Proposal - Tenant Query Gateway REST Routing (Retired Projection Actor)"
date: "2026-06-06"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "Tenant list/detail pages render no tenants; the read query 500s with 'did not find address for actor TenantsProjectionActor' at QueryRouter.RouteQueryAsync"
mode: "Incremental"
scope_classification: "Moderate"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-07T09:00:00+02:00"
  approval_note: "Direct Adjustment approved: re-point the BFF query gateway at GET /api/tenants* (architecture line 319-320) and emit server-side ETag/freshness per D6. Implement via story 3.5 (dev-later)."
affectedArtifacts:
  - "src/Hexalith.Tenants/Controllers/TenantsQueryController.cs"
  - "src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs"
  - "src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs"
  - "src/Hexalith.Tenants.UI/Program.cs"
  - "src/Hexalith.Tenants.Contracts/TenantProjectionRouting.cs"
  - "src/Hexalith.Tenants.AppHost/Program.cs"
  - "tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs"
  - "_bmad-output/planning-artifacts/architecture.md"
  - "_bmad-output/project-context.md"
  - "_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md"
  - "_bmad-output/implementation-artifacts/sprint-status.yaml"
---

# Sprint Change Proposal: Tenant Query Gateway REST Routing

## 1. Issue Summary

The Tenants UI renders no tenants on the list and detail surfaces. Now that per-user
authentication passes (a separate, already-fixed defect), the read query reaches the backend and
fails with HTTP 500:

```
did not find address for actor 'TenantsProjectionActor/tenant-index:system:index'
   at Hexalith.EventStore.Server.Queries.QueryRouter.RouteQueryAsync
```

**Discovery context:** found during Story 3.3 verification, after the auth-wiring and token-relay
fixes unblocked the request path. This is the third defect in a sequence (per-user login → circuit
token relay → **this query-routing defect**) that each masked the next.

**Root cause.** The UI BFF gateway routes tenant reads through the EventStore **generic** query
gateway and tags them with a retired projection actor:

- `TenantQueryGateway` builds a `SubmitQueryRequest` with
  `ProjectionActorType = TenantProjectionRouting.ActorTypeName` (`"TenantsProjectionActor"`) and
  sends it via `IEventStoreGatewayClient.SubmitQueryAsync` → `POST /api/v1/queries`
  (`CreateListQuery`/`CreateDetailQuery`/`CreateUserTenantsQuery`,
  `TenantQueryGateway.cs:264–352`; `TenantProjectionRouting.cs:12`).
- The EventStore `QueryRouter` invokes a **DAPR actor** by that type
  (`QueryRouter.cs:66–87`). That actor was **retired** during the domain-service SDK migration —
  the Tenants host now registers **in-process `IDomainQueryHandler`s** (`Program.cs:110–114`) and
  exposes reads via `TenantsQueryController` (`GET /api/tenants*`) and the SDK `/query` endpoint.
  No actor exists, so the call fails.

**This is implementation drift, not a design gap.** The architecture (Data Architecture, line
319–320) already mandates: *"a typed query gateway in the BFF wrapping the 5 REST endpoints"*
(`GET /api/tenants`, `/api/tenants/{id}`, `/api/tenants/{id}/users`, `/api/users/{id}/tenants`,
`/api/tenants/{id}/audit`). The BFF never adopted that transport and instead funnels through the
actor-based generic gateway.

## 2. Impact Analysis

### Epic Impact
- **Epic 3 (in-progress):** the read path that every command surface re-queries for projection
  confirmation is broken. Resolved by a dedicated fix story; **no scope change**.
- **Epics 1–2 (done):** their UI read surfaces were non-functional end-to-end against a live
  backend; this fix restores them. **No re-opening required** — the defect is in the shared BFF
  transport, not in per-story logic.
- **Epics 4–5 (backlog):** depend on the same query gateway → **unblocked** by this fix. No
  resequencing.

### Artifact Conflicts
- **PRD / Epics:** no conflict — no requirement changes.
- **Architecture:** **design unchanged** (the code violated it; the fix conforms). One additive
  regression-guard note recommended (see §4).
- **UX:** no change — the Truth State / freshness badge semantics are preserved by emitting
  ETag/projection-version per Data Architecture **D6**.
- **`_bmad-output/project-context.md:73`:** **stale** — states the host "registers only
  `TenantsProjectionActor`." Confirmed retired (`src/Hexalith.Tenants/Program.cs` registers
  `IDomainQueryHandler`s and maps `TenantsQueryController` + `/query`; no actor). Corrected here.

### Technical Impact
- All required changes are in **this repository** (`src/Hexalith.Tenants/*`, UI, AppHost) plus
  planning artifacts.
- The EventStore submodule is **only consumed** (`ReadModelEntry.ETag`, `QueryResponseMetadata`),
  **not modified** — the change respects the submodule policy.

## 3. Recommended Approach

**Option 1 — Direct Adjustment (SELECTED). Effort: Medium. Risk: Low.**

Bring the BFF query gateway into conformance with the documented architecture: call the Tenants
domain service's own REST endpoints (`GET /api/tenants*`) over DAPR service invocation, and emit
server-side ETag + freshness metadata from `TenantsQueryController` (per D6) sourced from the
read-model state-store ETag. Remove the dead projection-actor routing plumbing.

### Alternative considered and rejected

**Option B — keep the generic gateway; register Tenants query capability in the EventStore
gateway's `IDomainQueryHandlerRegistry` so `HandlerAwareQueryRouter` routes to the domain `/query`
endpoint instead of the actor.** Rejected because:

1. **Contradicts the architecture** (line 319–320): the BFF is specified to wrap the 5 REST
   endpoints, not the generic `POST /api/v1/queries` gateway.
2. **Breaks the freshness contract (D6).** `HandlerAwareQueryRouter` explicitly documents:
   *"Handler-based queries do not participate in projection ETag caching … `ProjectionType` is
   left null."* That yields **no ETag**, violating the chosen D6 freshness model
   (ETag/timestamp/projection-version → fail-closed `unknown`).
3. **Cross-repo coupling.** Requires EventStore-submodule + AppHost capability registration for a
   domain-local read concern — opposite of the domain-boundary policy.

**Option 2 (rollback)** and **Option 3 (MVP reduction)** are not applicable — no completed work is
the cause, and MVP scope is unaffected.

### MVP impact
None. This restores already-specified MVP behavior; it does not add or remove scope.

## 4. Detailed Change Proposals

### Code (this repository)

1. **`src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`** — emit a strong `ETag` header
   and freshness metadata (`ServedAt`, `ProjectionVersion`) on each 200, and honor `If-None-Match`
   → return **304 Not Modified** when the read-model ETag matches (server-side conditional, D6).

2. **`src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`** — surface the read-model
   ETag currently discarded in `GetStateAsync` (`ReadModelEntry.ETag`). ETag derives from the
   **primary read-model key** for the query: the tenant-index key for `ListTenants` /
   `GetUserTenants`; the per-tenant key for `GetTenant` / `GetTenantUsers` / `GetTenantAudit`.

3. **New BFF query client (UI, server-side):** a typed `HttpClient` (e.g.
   `ITenantsQueryApiClient`) that calls the five `GET /api/tenants*` endpoints, sends
   `If-None-Match`, reads the `ETag` + metadata, and returns payload + ETag + freshness. Replaces
   the `IEventStoreGatewayClient` query dependency for tenant reads.

4. **`src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`** — depend on the new
   client; keep the existing snapshot / freshness / degraded mapping; **delete**
   `CreateListQuery` / `CreateDetailQuery` / `CreateUserTenantsQuery` and the `SubmitQueryRequest`
   path.

5. **`src/Hexalith.Tenants.Contracts/TenantProjectionRouting.cs`** — **delete**; remove every
   `ProjectionActorType: TenantProjectionRouting.ActorTypeName` reference.

6. **`src/Hexalith.Tenants.UI/Program.cs`** — register the new typed client against
   `Tenants:BaseAddress` (DAPR service invocation) with bearer relay (the existing
   `AddGatewayAuthorization` pattern); keep `EventStore:BaseAddress` for the command gateway.

7. **`src/Hexalith.Tenants.AppHost/Program.cs`** — add
   `WithEnvironment("Tenants__BaseAddress", <tenants https endpoint>)` to the `tenants-ui`
   resource (it already `.WithReference(tenants)`).

8. **Tests** — update `TenantQueryGatewayTests` for the new client; add `TenantsQueryController`
   ETag/304 tests; add handler ETag-surfacing tests; assert no residual `ProjectionActorType`
   usage.

### Planning / documentation artifacts

9. **New fix story** `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md`.
10. **`_bmad-output/implementation-artifacts/sprint-status.yaml`** — add the fix story under Epic 3.
11. **`_bmad-output/planning-artifacts/architecture.md`** — add a regression-guard note: the
    EventStore generic gateway (`POST /api/v1/queries` + `HandlerAwareQueryRouter`) is **not** the
    Tenants UI transport; the BFF wraps `GET /api/tenants*`.
12. **`_bmad-output/project-context.md`** — correct the stale "registers only
    `TenantsProjectionActor`" statement (line 73) to reflect in-process `IDomainQueryHandler`
    dispatch via `TenantsQueryController` / the SDK `/query` endpoint.

## 5. Implementation Handoff

**Scope classification: Moderate.** Backlog entry + coordinated implementation.

- **Product Owner / Developer:** accept the fix story `3-5-tenant-query-gateway-rest-routing` into
  Epic 3; sprint-status updated to `ready-for-dev`.
- **Developer:** implement edits 1–8; satisfy the story acceptance criteria and tests; verify the
  list/detail/my-tenants/user-tenants/audit surfaces render against a live backend.
- **Tech writer / Developer:** apply doc edits 11–12 alongside the code change.

**Success criteria:**
- Tenant list/detail/my-tenants/user-tenants/audit render real data end-to-end (no actor 500).
- No `ProjectionActorType` / `TenantProjectionRouting` references remain.
- `TenantsQueryController` emits strong ETags + metadata and returns 304 on `If-None-Match` match.
- Freshness badge derives `current/aging/stale/unknown` from ETag/projection-version per D6;
  unmeasurable → `unknown` (fail-closed).
- Build is warning-clean (`TreatWarningsAsErrors`); Tier 1 + Tier 2 tests pass.
