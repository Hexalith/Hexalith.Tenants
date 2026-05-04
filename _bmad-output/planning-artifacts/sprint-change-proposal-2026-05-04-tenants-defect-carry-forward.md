# Sprint Change Proposal — 2026-05-04 — Tenants Defect Carry-Forward

**Author:** Jerome (via Correct Course workflow, MCP-observed E2E session)
**Scope:** Hexalith.Tenants only (no EventStore changes)
**Severity:** §A Critical, §B High
**Implementation effort:** ~6 h combined
**Routing:** Developer agent — direct implementation

---

## 1. Issue Summary

Two latent defects in `Hexalith.Tenants` discovered during MCP-observed end-to-end testing on 2026-05-04 against the running Aspire AppHost. Both regressions live in code that has been status `done` for weeks.

### §A — Tenants query API authentication broken (Critical)

`Hexalith.Tenants/src/Hexalith.Tenants/Program.cs` registers `[Authorize]`-protected controllers (`TenantsQueryController` and the `CommandsController` re-imported via `AddApplicationPart`) but never registers a JWT authentication scheme and never wires `UseAuthentication()` / `UseAuthorization()` into the middleware pipeline.

**Symptom:** every request to `https://localhost:61445/api/tenants/*` returns HTTP 500 with body:

> System.InvalidOperationException: No authenticationScheme was specified, and there was no DefaultChallengeScheme found.

**Read model is fully unreachable.** Discovered when querying a tenant freshly created via the command pipeline (`mcp-test-2518015b`) — command path returned 202 Completed (eventCount=1), query path returned 500.

### §B — DomainServiceRequestHandler fall-through misses MissingApplyMethodException (High)

`Hexalith.Tenants/src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs:60-62` uses substring matching to identify processor-mismatch fall-through:

```csharp
=> ex.Message.Contains("No Handle method found for command type", ...)
|| ex.Message.Contains("No matching Apply method found on state", ...);
```

The second substring never appears in any thrown exception. EventStore migrated state-rehydration failures to a typed `Hexalith.EventStore.Client.Aggregates.MissingApplyMethodException` (post-Epic-1 R1-A6), whose actual message format is `"Aggregate state '{0}' has no public void Apply({1}) method..."` — and the matcher does not detect it.

**Trigger condition:** any aggregate stream with persisted events whose Apply methods exist only on one aggregate's state class, where the *other* aggregate is tried first by DI iteration order.

Reproduced on stream `system|tenants|acme-corp`: `GlobalAdministratorsAggregate` tried first, fails to apply `TenantCreated` event, throws `MissingApplyMethodException`, matcher fails, request returns 500 instead of falling through to `TenantAggregate` which would correctly emit `TenantAlreadyExistsRejection`.

The existing test `DomainServiceRequestHandlerTests.cs:20` hand-throws a synthetic `InvalidOperationException` with the obsolete substring, so the production fall-through path has never been exercised against the real exception type.

## 2. Impact Analysis

### Epic Impact

| Bug | Parent epic | Status | Action |
|---|---|---|---|
| §A | Tenants Epic 5 — Tenant Discovery & Query (Story 5-3) | `done`, retrospective `optional` | Defect carry-forward story `post-epic-5-r5a1` |
| §B | Tenants Epic 2 — Core Tenant Management (DomainServiceRequestHandler used by Story 2-4) | `done`, retrospective `optional` | Defect carry-forward story `post-epic-2-r2a1` |

No epic restructuring. No new epic. No invalidation of future epics.

### Artifact Impact

- **PRD:** No change — both requirements remain valid; only implementations are faulty.
- **Architecture:** No change — convention-based dispatch and JWT-protected query API are still the documented design.
- **UI/UX:** No impact (no Admin UI page consumes `/api/tenants` yet).
- **Tests:** Two test updates inside the new stories — one Tier 2 integration test for §A, one Tier 1 unit test rewrite + one new Tier 1 test for §B.
- **CI / deployment / config:** No change — env vars already present.

### Test Coverage Gap

Both bugs escaped CI because:

- §A — no Tier 2 test asserts `/api/tenants` auth status codes.
- §B — the only existing test for fall-through hand-throws a synthetic exception with the obsolete message format, so it passes vacuously.

Both gaps are closed by the proposed stories.

## 3. Recommended Approach

**Direct Adjustment (Option 1).** Rejected: rollback (no recently completed work to revert), PRD MVP review (requirements are correct).

Rationale:

- Surgical fixes (~10 lines of source change combined).
- No coupling with active work (EventStore Epic 21 Fluent UI migration untouched).
- Converts silent regressions into CI-protected behavior via the test additions.
- Larger design improvement (typed-dispatch routing replacing substring matching) acknowledged as a backlog item but **not in scope** — it would expand to Moderate scope and risk regressing the working bootstrap path.

## 4. Detailed Change Proposals

### §A — `post-epic-5-r5a1-tenants-jwt-auth-wiring`

Source: `Hexalith.Tenants/src/Hexalith.Tenants/Program.cs`

```csharp
// Add (services):
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* bind from "Authentication:JwtBearer" section,
       matching EventStore ServiceCollectionExtensions.cs:68-89 */ });

// Add (middleware, before MapControllers):
app.UseAuthentication();
app.UseAuthorization();
```

Tests: new file `tests/Hexalith.Tenants.IntegrationTests/Auth/TenantsQueryAuthorizationTests.cs` — assert 401 with no JWT, 200 with admin JWT, 404 with valid JWT and unknown tenantId.

Full story: `Hexalith.Tenants/_bmad-output/implementation-artifacts/post-epic-5-r5a1-tenants-jwt-auth-wiring.md`.

### §B — `post-epic-2-r2a1-domain-processor-mismatch-matcher`

Source: `Hexalith.Tenants/src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs`

```csharp
// Replace the single catch with two ordered catches:
catch (MissingApplyMethodException) {
    logger.LogDebug("Skipping processor {ProcessorType} for command type {CommandType} (state-class mismatch)",
        processor.GetType().Name, request.Command.CommandType);
    continue;
}
catch (InvalidOperationException ex) when (IsProcessorMismatch(ex)) {
    logger.LogDebug("Skipping processor {ProcessorType} for command type {CommandType}",
        processor.GetType().Name, request.Command.CommandType);
}

// And remove the dead constant:
//   public const string MissingApplyMethodOnState = "No matching Apply method found on state";
// And simplify IsProcessorMismatch to only check MissingHandleMethod.
```

Tests:

- Update `DomainServiceRequestHandlerTests.cs:20` — throw real `MissingApplyMethodException` instead of synthetic `InvalidOperationException`.
- Add a new test with two registered aggregates whose state types differ; assert second processor is reached when first throws `MissingApplyMethodException`.

Full story: `Hexalith.Tenants/_bmad-output/implementation-artifacts/post-epic-2-r2a1-domain-processor-mismatch-matcher.md`.

### sprint-status.yaml

See `Hexalith.Tenants/_bmad-output/implementation-artifacts/sprint-status.yaml` — two new entries under their respective epics, both `ready-for-dev`.

## 5. Implementation Handoff

| Aspect | Detail |
|---|---|
| **Scope classification** | Minor |
| **Routed to** | Developer agent (direct implementation) |
| **Sequencing** | §A first (Critical), §B second (High). Independent — can also land in parallel. |
| **Effort estimate** | §A ≤ 2 h; §B ≤ 4 h (incl. Tier 1 + Tier 2 tests). |
| **Success criteria** | (1) MCP E2E reproducer rerun on `mcp-test-2518015b` returns 200 from `GET /api/tenants/{id}`. (2) Same reproducer on `acme-corp` returns `TenantAlreadyExistsRejection`, not 500. (3) New tests in CI green. (4) All existing Tier 1 + Tier 2 + Tier 3 tests still pass. |
| **Backlog follow-up** | Replace exception-driven fall-through in `DomainServiceRequestHandler` with explicit aggregate-id routing — out of scope for this proposal. |

## 6. Approval

- [x] Issue trigger and evidence captured (Section 1)
- [x] Epic impact assessed (Section 2)
- [x] Artifact impact assessed (Section 3)
- [x] Path forward selected with rationale (Section 4)
- [x] User approval (2026-05-04, Jerome)
- [x] sprint-status.yaml updated
- [x] Story files created
- [ ] Source changes implemented
- [ ] CI green

---

*Generated 2026-05-04 by Correct Course workflow. Trigger: MCP-observed E2E test session against running AppHost.*
