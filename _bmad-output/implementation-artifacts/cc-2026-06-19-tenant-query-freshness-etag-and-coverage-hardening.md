---
title: 'Tenant query freshness, ETag, and coverage hardening'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'ready-for-dev'
sprint_key: 'cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md'
approval: 'Administrator approved sprint-change-proposal-2026-06-19-deferred-work.md on 2026-06-19'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - do not expand without re-approval">

## Intent

Harden the REST-backed Tenants query path after Story 3.5 by making freshness truthful, making ETag behavior explicit and robust, restoring state-store reconstruction coverage on the production REST/handler path, and adding support-safety assertions for live gateway error mapping.

## Boundaries & Constraints

**Always:**
- Keep Tenants UI reads on the REST-backed Tenants domain endpoints. Do not reintroduce `TenantsProjectionActor`, `TenantProjectionRouting`, or an EventStore generic query-gateway route for tenant reads.
- Use `Hexalith.EventStore` persistence/read-model abstractions. If generic projection age or read-model metadata support is missing, consume or propose it in EventStore instead of adding generic persistence scaffolding to Tenants.
- Keep domain identifiers as caller-supplied strings, not GUIDs or ULIDs.
- Keep all user-facing failure copy support-safe: no raw payloads, stack traces, tokens, ETags, cursors, correlation IDs, or reason-code internals.

**Never:**
- Do not treat response time as projection age.
- Do not claim `aging` or `stale` unless backed by a real persisted projection timestamp/version or an explicitly documented direct-read rule.
- Do not restore the retired actor-based stateless restart test. Add REST/handler production-path coverage instead.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.Server/Projections/*ReadModel.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/*`
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/*`

## Tasks & Acceptance

**Execution:**
- [ ] Audit current query metadata flow from read-model store through handler result, `TenantsQueryController`, REST client, and `TenantQueryGateway.ResolveFreshness`.
- [ ] Decide and implement the D6 freshness model: persisted projection age/version if available, shared EventStore capability if needed, or an explicitly documented direct-read `current` / unknown fallback.
- [ ] Harden null/whitespace ETag behavior and document the 200/no-ETag/no-304 path.
- [ ] Harden `If-None-Match` normalization for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
- [ ] Add live gateway support-safety tests for populated `correlationId` / `reasonCode` paths.
- [ ] Replace the deleted actor reconstruction coverage with REST/handler persisted read-model reconstruction coverage.
- [ ] Re-run focused suites and record any remaining Tier 2/Tier 3 blockers with exact current evidence.
- [ ] Update `deferred-work.md` and test-summary artifacts after implementation so stale Story 3.5 blockers are not carried forward.

**Acceptance Criteria:**
1. Given a successful tenant query response, when freshness metadata is emitted, then `ServedAt` is not used as a proxy for projection age unless it is backed by persisted projection metadata; otherwise the response reports freshness as `unknown` or uses an explicitly documented direct-read `current` rule.
2. Given D6 freshness states, when a real persisted projection timestamp/version is available, then `ResolveFreshness` can produce `current`, `aging`, `stale`, and `unknown` according to configurable thresholds with tests for each state.
3. Given the current `IReadModelStore` only exposes `Value` + `ETag`, when implementation needs generic read-model metadata, then the developer either consumes a shared EventStore capability or records an EventStore handoff instead of adding generic persistence scaffolding to Tenants.
4. Given a read-model ETag is null or whitespace, when a REST query succeeds, then the response behavior is explicit and tested: 200 with no ETag and no 304 support, and UI freshness fails closed unless a real projection marker exists.
5. Given `If-None-Match` contains weak tags, `*`, escaped strong tags, or unsupported multi-tag input, when the server/client normalizes it, then unsupported input maps to a safe non-leaking query state and supported strong tags compare consistently with the emitted strong ETag.
6. Given a gateway error response includes `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, or ETags, when `TenantQueryGateway` maps it to UI snapshots, then rendered/user-facing copy excludes those values on the live populated-correlation path.
7. Given the retired actor path was removed, when integration coverage runs, then a REST/handler equivalent proves persisted read-model state survives a fresh service instance or handler/store boundary. Do not restore the retired projection actor test.
8. Given Tier 2 and Tier 3 evidence, when full suites remain blocked, then the story records exact current blockers; if the prior pubsub/health blockers are now resolved, `deferred-work.md` is updated to remove stale blocker text.

## Verification

Run focused tests first, then broaden only as needed:

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter Query`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantQuery`
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-restore --filter StatelessRestart`
- `git diff --check`

## Dev Agent Record

Pending implementation.
