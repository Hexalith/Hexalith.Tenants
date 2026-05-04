# Story: post-epic-5-r5a1 — Tenants JWT Authentication Pipeline Wiring

**Epic:** Tenants Epic 5 — Tenant Discovery & Query (defect carry-forward)
**Status:** ready-for-dev
**Severity:** Critical — `/api/tenants/*` returns 500 for all requests
**Source proposal:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-04-tenants-defect-carry-forward.md` §A

## Context

`Hexalith.Tenants/src/Hexalith.Tenants/Program.cs` registers controllers carrying
`[Authorize]` (`TenantsQueryController`, plus `CommandsController` from EventStore
via `AddApplicationPart`) but never registers a JWT authentication scheme and
never calls `UseAuthentication()` / `UseAuthorization()` in the middleware
pipeline. Every request to a protected endpoint hits `AuthenticationMiddleware`
with no default challenge scheme and ASP.NET Core throws:

> System.InvalidOperationException: No authenticationScheme was specified, and
> there was no DefaultChallengeScheme found.

Discovered 2026-05-04 during MCP-observed E2E testing. Reproducible: any
authenticated GET to `https://localhost:61445/api/tenants` returns 500.

## Acceptance Criteria

1. `Program.cs` registers JWT Bearer authentication using the same options
   contract already wired for EventStore
   (`Authentication__JwtBearer__{Authority,Audience,Issuer,SigningKey,RequireHttpsMetadata}`).
   Configuration values already populated by AppHost.
2. Middleware pipeline includes `UseAuthentication()` then `UseAuthorization()`
   in the correct order, before `MapControllers()`.
3. Tier 2 integration test added: `GET /api/tenants` with no Authorization
   header → 401; with valid JWT (admin-user) → 200.
4. Tier 2 integration test added: `GET /api/tenants/{id}` with no auth → 401;
   valid JWT but unknown tenantId → 404; valid JWT and existing tenant → 200.
5. All existing Tier 1 + Tier 2 tests still pass.
6. No env var or appsettings additions required (config already present).

## Implementation Notes

- Reuse the existing extension pattern from EventStore
  (`src/Hexalith.EventStore/Extensions/ServiceCollectionExtensions.cs:68-89`)
  if it can be referenced, otherwise inline equivalent registration in Tenants
  to avoid a cross-project public-API contract.
- Keep `app.MapDefaultEndpoints()` ordering intact.
- Do NOT modify `appsettings.json` — values are already correct.

## Test Plan

- Unit: N/A (config wiring).
- Tier 2: new file `tests/Hexalith.Tenants.IntegrationTests/Auth/TenantsQueryAuthorizationTests.cs`
  asserting 401/200/404 matrix above using the Tier 2 fixture
  (`TenantsDaprTestFixture`).
- Manual: rerun MCP E2E reproducer from 2026-05-04 — expect `GET /api/tenants` → 200.
