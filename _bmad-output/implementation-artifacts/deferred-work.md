# Deferred Work

## Deferred from: implementation readiness correction (2026-05-13)

- D13-D17 UX-driven architecture amendments are Phase 2 Admin UI / FrontShell reference-module work unless explicitly promoted by a future scope decision.
- Deferred items include SignalR three-phase UI confirmation, FrontShell `pendingIds`, concurrent command support, toast batching, `<AuditTimeline>`, `<ConsequencePreview>`, FrontShell design tokens, and UI `blockedBy` sequencing.
- Backend MVP work must still satisfy D11 query-side authorization and D12 audit query requirements because those map to PRD FR28/FR29 and NFR5.

## Deferred from: code review of post-epic-5-r5a1-tenants-jwt-auth-wiring (2026-05-13)

- AC1 implementation note advised inlining the JWT auth registration in Tenants "to avoid a cross-project public-API contract"; dev chose to import `Hexalith.EventStore.Authentication` types directly. Deferred — **acceptable coupling** (Hexalith.EventStore.Authentication treated as a stable shared contract; duplicating the registration would create drift risk between Tenants and EventStore).
- Production `appsettings.json` ships with empty `Authority` and `SigningKey` under `Authentication:JwtBearer`. `Program.cs` now calls `.ValidateOnStart()` on `EventStoreAuthenticationOptions`, which rejects this config combination unless AppHost / environment variables override it at runtime. Pre-existing; the spec (AC6) explicitly forbids appsettings additions in this story. Audit deployment config to confirm overrides are in place before the next release.
- The `eventstore:tenant` claim is used by the EventStore rate-limiter to partition request buckets and is asserted by both `TestAuthHandler` and the new test JWTs in `TenantsQueryControllerIntegrationTests`. If the production IdP does not emit this claim, authenticated users fall into a shared `"anonymous"` rate-limit bucket — silent throttling under load. Pre-existing concern, shared with EventStore; track separately as part of the IdP claim contract.
