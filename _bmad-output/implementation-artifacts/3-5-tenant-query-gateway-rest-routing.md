---
baseline_commit: 3cbdeab
created: 2026-06-06T12:30:00+02:00
type: defect-fix
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06-tenant-query-routing.md
---

# Story 3.5: Tenant Query Gateway REST Routing (Retire Projection-Actor Path)

Status: ready-for-dev

<!-- Note: Defect-fix story created by the BMAD correct-course workflow. Not in the canonical
     epics.md feature list; tracked under Epic 3 as a blocking read-path fix. -->

## Story

As a Tenants UI user,
I want the tenant list, detail, my-tenants, user-tenants, and audit surfaces to load real data,
so that I can triage and manage tenants instead of seeing a failed/empty read surface.

## Context

The UI BFF routes tenant reads through the EventStore **generic** query gateway
(`IEventStoreGatewayClient.SubmitQueryAsync` → `POST /api/v1/queries`), tagging each request with
`ProjectionActorType = "TenantsProjectionActor"`. `QueryRouter` invokes that DAPR actor, but it was
retired during the domain-service SDK migration — the Tenants host now serves reads in-process via
`TenantsQueryController` (`GET /api/tenants*`) → `DomainQueryDispatcher` → `TenantQueryHandlerBase`.
The query therefore 500s with *"did not find address for actor 'TenantsProjectionActor/...'"*.

The architecture (Data Architecture, line 319–320) mandates a BFF query gateway that **wraps the 5
REST endpoints**. This story brings the implementation into conformance and adds the D6
ETag/freshness contract on the server side. See the Sprint Change Proposal for full analysis and
the rejected alternative (registering capability with `HandlerAwareQueryRouter`, which drops ETags
and violates D6).

## Acceptance Criteria

1. Given an authenticated user opens the tenant list, when the list loads, then the BFF calls
   `GET /api/tenants` on the Tenants domain service (DAPR service invocation, server-side, bearer
   relayed) and renders the user's authorized tenants — no projection-actor invocation occurs and
   no `did not find address for actor` error is produced.
2. Given the tenant detail, my-tenants, user-tenants, and audit surfaces, when each loads, then the
   BFF calls the matching REST endpoint (`GET /api/tenants/{id}`, `GET /api/users/{id}/tenants`,
   `GET /api/tenants/{id}/users`, `GET /api/tenants/{id}/audit`) and renders authorized data with
   the existing six list/detail states preserved.
3. Given `TenantsQueryController` serves a successful read, when it responds, then it sets a strong
   `ETag` header and freshness metadata (`ServedAt`, `ProjectionVersion`) derived from the primary
   read-model state-store ETag (tenant-index key for list/user-tenants; per-tenant key for
   detail/users/audit).
4. Given a client resends a request with `If-None-Match` equal to the current read-model ETag, when
   the controller evaluates it, then it returns **304 Not Modified** with the `ETag` header and no
   body, and the UI preserves its last-confirmed snapshot (non-collapse).
5. Given the freshness badge, when a response carries an ETag/projection-version, then the badge
   derives `current/aging/stale/unknown` per D6; when freshness is unmeasurable, then it fails
   closed to `unknown` — it never reports a fabricated `current`.
6. Given the codebase after the fix, when searched, then no `ProjectionActorType` assignment and no
   reference to `TenantProjectionRouting` remains; `TenantProjectionRouting.cs` is deleted.
7. Given the AppHost and UI host, when the UI starts, then it is configured with
   `Tenants:BaseAddress` (or DAPR app reference) for the query client and continues to use
   `EventStore:BaseAddress` for the command gateway; when neither query route is configured, the UI
   falls back to `UnavailableTenantQueryGateway` (existing fail-closed behavior).
8. Given support-safety rules, when reads succeed or fail, then ETags, cursors, raw payloads,
   tokens, correlation ids, and stack traces are never surfaced to the user or placed in live
   regions/logs/copied text.
9. Given verification is run, then Tier 1 + Tier 2 tests pass and the build is warning-clean under
   `TreatWarningsAsErrors`.

## Tasks / Subtasks

- [ ] Emit server-side ETag + freshness on the REST query controller (AC: 3, 4, 8)
  - [ ] In `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`, after a successful
        dispatch, set a strong `ETag` response header and return freshness metadata
        (`ServedAt`, `ProjectionVersion`) for each of the 5 endpoints.
  - [ ] Honor the `If-None-Match` request header: when it matches the current read-model ETag,
        short-circuit to `304 Not Modified` (with the `ETag` header, no body) before serializing
        the payload.
  - [ ] Keep the existing RBAC gate, identifier validation, opaque-cursor validation, and
        problem-details mapping unchanged. Never leak the ETag/cursor/correlation id in user copy.

- [ ] Surface the read-model ETag from the query handlers (AC: 3)
  - [ ] In `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`, stop discarding
        `ReadModelEntry.ETag` in `GetStateAsync`; expose the primary read-model key's ETag so the
        controller can build the response ETag (index key for `ListTenants`/`GetUserTenants`;
        per-tenant key for `GetTenant`/`GetTenantUsers`/`GetTenantAudit`).
  - [ ] Do not change the in-process dispatch contract used by the SDK `/query` endpoint beyond
        what is needed to carry the ETag/version.

- [ ] Add the server-side BFF query client (AC: 1, 2, 5, 8)
  - [ ] Add a typed `HttpClient` query client (e.g. `ITenantsQueryApiClient` +
        implementation) under `src/Hexalith.Tenants.UI/Services/Gateways/` that calls the 5
        `GET /api/tenants*` endpoints, sends `If-None-Match`, reads the `ETag` + metadata, and
        returns payload + ETag + freshness (reuse `Hexalith.Tenants.Client`/`.Contracts` DTOs:
        `PaginatedResult<T>`, `TenantSummary`, `TenantDetail`, `TenantMember`,
        `UserTenantMembership`, `TenantAuditEntry`).
  - [ ] Map non-success status codes (401/403/404/400/503) to the existing gateway exception /
        snapshot states so the six surface states and fail-closed gating are preserved.

- [ ] Re-point the tenant query gateway and remove dead actor plumbing (AC: 1, 2, 6)
  - [ ] In `TenantQueryGateway.cs`, replace the `IEventStoreGatewayClient` query dependency with
        the new client; keep the snapshot/freshness/degraded mapping logic.
  - [ ] Delete `CreateListQuery`/`CreateDetailQuery`/`CreateUserTenantsQuery` and the
        `SubmitQueryRequest` construction.
  - [ ] Delete `src/Hexalith.Tenants.Contracts/TenantProjectionRouting.cs` and all references.

- [ ] Wire the query route through DI and the AppHost (AC: 1, 7)
  - [ ] In `src/Hexalith.Tenants.UI/Program.cs`, register the typed query client against
        `Tenants:BaseAddress` (DAPR service invocation) with bearer relay; bind
        `ITenantQueryGateway` to the REST-backed `TenantQueryGateway`. Keep the
        `UnavailableTenantQueryGateway` fallback when the query route is not configured.
  - [ ] In `src/Hexalith.Tenants.AppHost/Program.cs`, add
        `WithEnvironment("Tenants__BaseAddress", <tenants https endpoint>)` to the `tenants-ui`
        resource (it already `.WithReference(tenants)`).

- [ ] Tests and evidence (AC: 1-9)
  - [ ] Update `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` for
        the new client (list/detail/my-tenants/user-tenants/audit happy paths, 304/not-modified
        snapshot preservation, ETag handling, error-to-state mapping, unavailable fallback).
  - [ ] Add `TenantsQueryController` tests: ETag emitted on 200, `If-None-Match` match → 304,
        mismatch → 200 with new ETag, RBAC/identifier/cursor paths unchanged.
  - [ ] Add handler tests proving the primary read-model ETag is surfaced per query type.
  - [ ] Add a guard assertion that no `ProjectionActorType`/`TenantProjectionRouting` reference
        remains in the UI/Contracts.
  - [ ] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and
        `tests/test-summary.md` per the current evidence practice.

- [ ] Documentation alignment (AC: n/a — proposal items 11-12)
  - [ ] Add the regression-guard note to `architecture.md` (generic gateway is not the Tenants UI
        transport; BFF wraps `GET /api/tenants*`).
  - [ ] Correct `_bmad-output/project-context.md` line 73 (retired `TenantsProjectionActor`).

## Dev Notes

- **Boundary:** all code changes are in this repo. Do **not** modify the `Hexalith.EventStore`
  submodule; `ReadModelEntry.ETag` and `QueryResponseMetadata` are consumed read-only.
- **Transport:** server-side BFF over DAPR service invocation to the `tenants` app. This does not
  violate the "no browser-side HttpClient to /api/tenants" rule — the call originates in the BFF.
- **ETag source:** `ReadModelEntry.ETag` is the DAPR state-store ETag (`IReadModelStore.GetAsync`),
  already registered via `AddEventStoreReadModelStore()` in the Tenants host.
- **Do not** re-introduce a projection actor or register Tenants capability with the EventStore
  gateway's `HandlerAwareQueryRouter` (drops ETags → violates D6).
