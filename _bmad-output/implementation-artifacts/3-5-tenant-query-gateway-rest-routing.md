---
baseline_commit: 3cbdeab
created: 2026-06-06T12:30:00+02:00
type: defect-fix
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06-tenant-query-routing.md
---

# Story 3.5: Tenant Query Gateway REST Routing (Retire Projection-Actor Path)

Status: review

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

- [x] Emit server-side ETag + freshness on the REST query controller (AC: 3, 4, 8)
  - [x] In `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`, after a successful
        dispatch, set a strong `ETag` response header and return freshness metadata
        (`ServedAt`, `ProjectionVersion`) for each of the 5 endpoints.
  - [x] Honor the `If-None-Match` request header: when it matches the current read-model ETag,
        short-circuit to `304 Not Modified` (with the `ETag` header, no body) before serializing
        the payload.
  - [x] Keep the existing RBAC gate, identifier validation, opaque-cursor validation, and
        problem-details mapping unchanged. Never leak the ETag/cursor/correlation id in user copy.

- [x] Surface the read-model ETag from the query handlers (AC: 3)
  - [x] In `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`, stop discarding
        `ReadModelEntry.ETag` in `GetStateAsync`; expose the primary read-model key's ETag so the
        controller can build the response ETag (index key for `ListTenants`/`GetUserTenants`;
        per-tenant key for `GetTenant`/`GetTenantUsers`/`GetTenantAudit`).
  - [x] Do not change the in-process dispatch contract used by the SDK `/query` endpoint beyond
        what is needed to carry the ETag/version.

- [x] Add the server-side BFF query client (AC: 1, 2, 5, 8)
  - [x] Add a typed `HttpClient` query client (e.g. `ITenantsQueryApiClient` +
        implementation) under `src/Hexalith.Tenants.UI/Services/Gateways/` that calls the 5
        `GET /api/tenants*` endpoints, sends `If-None-Match`, reads the `ETag` + metadata, and
        returns payload + ETag + freshness (reuse `Hexalith.Tenants.Client`/`.Contracts` DTOs:
        `PaginatedResult<T>`, `TenantSummary`, `TenantDetail`, `TenantMember`,
        `UserTenantMembership`, `TenantAuditEntry`).
  - [x] Map non-success status codes (401/403/404/400/503) to the existing gateway exception /
        snapshot states so the six surface states and fail-closed gating are preserved.

- [x] Re-point the tenant query gateway and remove dead actor plumbing (AC: 1, 2, 6)
  - [x] In `TenantQueryGateway.cs`, replace the `IEventStoreGatewayClient` query dependency with
        the new client; keep the snapshot/freshness/degraded mapping logic.
  - [x] Delete `CreateListQuery`/`CreateDetailQuery`/`CreateUserTenantsQuery` and the
        `SubmitQueryRequest` construction.
  - [x] Delete `src/Hexalith.Tenants.Contracts/TenantProjectionRouting.cs` and all references.

- [x] Wire the query route through DI and the AppHost (AC: 1, 7)
  - [x] In `src/Hexalith.Tenants.UI/Program.cs`, register the typed query client against
        `Tenants:BaseAddress` (DAPR service invocation) with bearer relay; bind
        `ITenantQueryGateway` to the REST-backed `TenantQueryGateway`. Keep the
        `UnavailableTenantQueryGateway` fallback when the query route is not configured.
  - [x] In `src/Hexalith.Tenants.AppHost/Program.cs`, add
        `WithEnvironment("Tenants__BaseAddress", <tenants https endpoint>)` to the `tenants-ui`
        resource (it already `.WithReference(tenants)`).

- [x] Tests and evidence (AC: 1-9)
  - [x] Update `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` for
        the new client (list/detail/my-tenants/user-tenants/audit happy paths, 304/not-modified
        snapshot preservation, ETag handling, error-to-state mapping, unavailable fallback).
  - [x] Add `TenantsQueryController` tests: ETag emitted on 200, `If-None-Match` match → 304,
        mismatch → 200 with new ETag, RBAC/identifier/cursor paths unchanged.
  - [x] Add handler tests proving the primary read-model ETag is surfaced per query type.
  - [x] Add a guard assertion that no `ProjectionActorType`/`TenantProjectionRouting` reference
        remains in the UI/Contracts.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and
        `tests/test-summary.md` per the current evidence practice.

- [x] Documentation alignment (AC: n/a — proposal items 11-12)
  - [x] Add the regression-guard note to `architecture.md` (generic gateway is not the Tenants UI
        transport; BFF wraps `GET /api/tenants*`).
  - [x] Correct `_bmad-output/project-context.md` line 73 (retired `TenantsProjectionActor`).

## Dev Notes

- **Boundary:** all code changes are in this repo. Do **not** modify the `Hexalith.EventStore`
  submodule; `ReadModelEntry.ETag` and `QueryResponseMetadata` are consumed read-only.
- **Transport:** server-side BFF over DAPR service invocation to the `tenants` app. This does not
  violate the "no browser-side HttpClient to /api/tenants" rule — the call originates in the BFF.
- **ETag source:** `ReadModelEntry.ETag` is the DAPR state-store ETag (`IReadModelStore.GetAsync`),
  already registered via `AddEventStoreReadModelStore()` in the Tenants host.
- **Do not** re-introduce a projection actor or register Tenants capability with the EventStore
  gateway's `HandlerAwareQueryRouter` (drops ETags → violates D6).

## Dev Agent Record

### Implementation Plan

- Replace the UI read path with a typed REST query client while preserving the existing gateway snapshot, freshness, and degraded-state model.
- Carry primary read-model ETags from domain query handlers into `TenantsQueryController`, emit freshness headers, and honor strong `If-None-Match`.
- Retire projection-actor routing references and add tests/evidence proving the REST route is the only Tenants UI query path.

### Debug Log

- Added `TenantQueryResult` metadata and handler ETag propagation, then wired `TenantsQueryController` to emit `ETag`, `X-Hexalith-Projection-Version`, and `X-Hexalith-Served-At`.
- Preserved controller RBAC validation by explicitly running `ITenantValidator` and `IRbacValidator` before dispatching direct REST reads.
- Reworked `TenantQueryGateway` to call `ITenantsQueryApiClient` over `GET /api/tenants*` paths, keeping previous snapshot preservation for 304 responses.
- Updated integration-test query-handler adapters so `TenantsQueryControllerIntegrationTests` exercise in-process handlers without the retired projection actor path.
- Validation passed for affected builds and focused/full executable suites listed in the evidence summary. The story-owned AC9 signal is warning-clean, but full Server.Tests remains blocked by existing documentation/AppHost checks for missing pubsub evidence and full IntegrationTests remains blocked by unrelated health-readiness contract drift.

### Completion Notes

- Tenant list, detail, my-tenants, user-tenants, global-administrators, and audit UI reads now use REST-backed Tenants domain-service endpoints instead of the EventStore generic query gateway.
- REST query responses now expose strong ETags/freshness metadata derived from primary read-model state-store ETags and return 304 without a body on matching `If-None-Match`.
- Dead tenant projection-routing contract was deleted, DI/AppHost now provide `Tenants:BaseAddress`, and source guards prevent reintroducing tenant projection-actor routing symbols in UI/Contracts.

## File List

- `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-context.md`
- `src/Hexalith.Tenants.AppHost/Program.cs`
- `src/Hexalith.Tenants.Contracts/TenantProjectionRouting.cs` (deleted)
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsQueryApiClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiRequest.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantUsersQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsQueryApiClientTests.cs`
- `tests/test-summary.md`

## Change Log

- 2026-06-07T10:38:21+02:00 - Implemented REST-backed tenant query routing, ETag/freshness handling, DI/AppHost wiring, retired projection-actor routing, and added tests/evidence for Story 3.5.
