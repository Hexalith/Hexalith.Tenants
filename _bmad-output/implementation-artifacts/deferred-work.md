# Deferred Work

## Deferred from: implementation readiness correction (2026-05-13)

- D13-D17 UX-driven architecture amendments are Phase 2 Admin UI / FrontShell reference-module work unless explicitly promoted by a future scope decision.
- Deferred items include SignalR three-phase UI confirmation, FrontShell `pendingIds`, concurrent command support, toast batching, `<AuditTimeline>`, `<ConsequencePreview>`, FrontShell design tokens, and UI `blockedBy` sequencing.
- Backend MVP work must still satisfy D11 query-side authorization and D12 audit query requirements because those map to PRD FR28/FR29 and NFR5.

## Deferred from: code review of post-epic-5-r5a3-tenant-audit-projection-query (2026-05-15)

- Concurrent `SaveStateAsync` writes have no etag/optimistic-concurrency on `audit:{tenantId}`, `projection:tenants:{tenantId}`, or `projection:tenant-index:singleton`. Inherited last-writer-wins pattern. Worth a project-wide concurrency story for all read-model writes.
- Cursor format `Ticks:EventId` (audit) and `key` (other paginated endpoints) is plain-text and trivially forgeable. Cursor opaqueness / HMAC signing is a cross-cutting concern across `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`.
- `pageSize` clamping duplicated in `TenantsQueryController.ClampAuditPageSize` (default 100 / max 1000) and `TenantsProjectionActor.DeserializeAuditPayload` (identical bounds). Defensive consistency; harmonize via shared constant only on next refactor of these clamps.
- CancellationToken not threaded through `HandleGetTenantAuditAsync` / `TenantProjectionHandler.ProjectAsync`. Long-running audit reads/writes cannot be cancelled. Requires modifying `CachingProjectionActor.ExecuteQueryAsync` signature in the EventStore submodule and the `ProjectionDispatcher` minimal-API endpoint. Cross-cutting submodule refactor; track as a hardening item across all `Handle*` methods in `TenantsProjectionActor` and both projection handlers.

## Deferred from: code review of post-epic-5-r5a2-get-user-tenants-scoped-authorization (2026-05-14)

- Cursor stability under concurrent role mutation — cross-user queries now go through filtered pagination; if a requester's `TenantOwner` role on a cursor tenant is revoked between page fetches, `Paginate`'s lexicographic `Where(key > cursor)` may skip a newly-visible tenant or advance past a now-hidden one. Same property exists in `list-tenants`. Track with the broader read-model cursor stability story.
- No defense-in-depth `envelope.UserId` non-empty check at actor layer — consistent with all other `Handle*` methods in `TenantsProjectionActor`; controller-layer auth is the primary guard. Worth tracking as a broader actor-surface hardening item.
- `TenantStatus.Disabled` tenants surfaced via TenantOwner-scoped lookup — spec silent on filtering inactive/disabled tenants. Confirm desired policy with product before next release.
- Demotion race window slightly widened — admin lookup now runs *after* the index load (pre-diff order had admin check first). Negligible operational risk; revoked admin sees full target memberships in the brief race window.
- Orphan membership in `UserTenants` but missing from `Tenants` map produces blank-name (`""`) entries — pre-existing fallback `entry?.Name ?? string.Empty`. `Apply` guards in the projection drop orphan adds in normal flow.
- Self-lookup with stale projection — read-model eventual consistency means a user removed from all tenants may briefly still see pre-removal memberships. Pre-existing.
- `StringComparer.Ordinal` is the project-wide pattern for tenant/user ID comparisons — canonicalization (e.g., lowercasing `sub`) is expected at the auth boundary. Track as a project-wide consistency item, not a per-story patch.

## Deferred from: code review of post-epic-5-r5a1-tenants-jwt-auth-wiring (2026-05-13)

- AC1 implementation note advised inlining the JWT auth registration in Tenants "to avoid a cross-project public-API contract"; dev chose to import `Hexalith.EventStore.Authentication` types directly. Deferred — **acceptable coupling** (Hexalith.EventStore.Authentication treated as a stable shared contract; duplicating the registration would create drift risk between Tenants and EventStore).
- Production `appsettings.json` ships with empty `Authority` and `SigningKey` under `Authentication:JwtBearer`. `Program.cs` now calls `.ValidateOnStart()` on `EventStoreAuthenticationOptions`, which rejects this config combination unless AppHost / environment variables override it at runtime. Pre-existing; the spec (AC6) explicitly forbids appsettings additions in this story. Audit deployment config to confirm overrides are in place before the next release.
- The `eventstore:tenant` claim is used by the EventStore rate-limiter to partition request buckets and is asserted by both `TestAuthHandler` and the new test JWTs in `TenantsQueryControllerIntegrationTests`. If the production IdP does not emit this claim, authenticated users fall into a shared `"anonymous"` rate-limit bucket — silent throttling under load. Pre-existing concern, shared with EventStore; track separately as part of the IdP claim contract.
