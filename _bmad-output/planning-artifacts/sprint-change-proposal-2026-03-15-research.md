# Sprint Change Proposal — Apply Research Findings to Planning Artifacts

**Date:** 2026-03-15
**Author:** Jerome (via Correct Course workflow)
**Change Scope:** Minor (documentation enrichment within existing stories)
**Status:** APPROVED

---

## 1. Issue Summary

**Trigger:** Deep technical research conducted on 2026-03-15 analyzing the Hexalith.EventStore submodule source code to prepare for Stories 2.2–2.4 implementation. The research uncovered implementation details not captured in the current architecture and epics documents.

**Finding:** The EventStore framework uses a DAPR service-to-service invocation pattern where the AggregateActor delegates domain processing back to the CommandApi via a `/process` endpoint. Additionally, the research provides concrete implementation blueprints (state classes, aggregate base classes, testing patterns with `ProcessAsync` + `CommandEnvelope`) and identifies a DAPR SDK version drift (Tenants at 1.16.1, EventStore at 1.17.3). These details are critical for Story 2.2–2.4 implementers but were not in the planning artifacts.

**Evidence:**

- Research document: `_bmad-output/planning-artifacts/research/technical-hexalith-eventstore-tenant-implementation-research-2026-03-15.md`
- EventStore source analysis: `AggregateActor.cs` (5-step pipeline), `DaprDomainServiceInvoker.cs` (service-to-service call), `EventStoreAggregate.cs` (ProcessAsync API)
- DAPR version drift: `Directory.Packages.props` has 1.16.1, EventStore submodule uses 1.17.3
- All 24 contract tests pass, build succeeds — no code changes needed

---

## 2. Impact Analysis

### Epic Impact

| Epic | Status | Impact |
| ---- | ------ | ------ |
| Epic 1 (Project Foundation) | Done | None — fully compatible |
| Epic 2 (Core Tenant Management) | In-progress | 3 stories enriched with implementation blueprints (2.2, 2.3, 2.4) |
| Epic 3 (Membership, Roles, Config) | Backlog | None — patterns established in Epic 2 carry forward |
| Epic 4 (Event-Driven Integration) | Backlog | None |
| Epic 5 (Tenant Discovery & Query) | Backlog | Note: SignalR projection change notifications documented in architecture for future reference |
| Epic 6 (Testing) | Backlog | None — research testing pattern aligns with existing conformance test design |
| Epic 7 (Deployment & Observability) | Backlog | None |
| Epic 8 (Documentation) | Backlog | None |

No epics added, removed, or resequenced. Dependency chain unchanged.

### Artifact Changes Applied

**Architecture document (`architecture.md`) — 3 changes:**

1. **D9 Revision: Command Processing Pipeline** — Replaced simplified "Data Flow" section with detailed pipeline showing HTTP → MediatR → CommandRouter → AggregateActor (5-step checkpointed pipeline) → DaprDomainServiceInvoker → CommandApi `/process` endpoint → IDomainProcessor → Handle method. Includes query flow with ETag pre-check and projection flow with DaprProjectionChangeNotifier.

2. **D10: Aggregate Testing Blueprint** — New section documenting the `ProcessAsync` + `CommandEnvelope` testing pattern with concrete code example. Defines test categories (success paths, rejection paths, NoOp paths, Apply methods, state replay) — all Tier 1 with zero infrastructure.

3. **DAPR version alignment** — Updated all DAPR SDK version references from 1.17.0 to 1.17.3 to match EventStore submodule.

**Epics document (`epics.md`) — 3 story enrichments:**

1. **Story 2.2 (GlobalAdmin Aggregate)** — Added implementation blueprint: `GlobalAdministratorsState` class shape with Apply methods, aggregate base class (`EventStoreAggregate<GlobalAdministratorsState>`), 3 Handle method behaviors, last-admin protection logic, testing pattern reference.

2. **Story 2.3 (Tenant Aggregate)** — Added implementation blueprint: `TenantState` class shape with 9 properties and 9 Apply methods, aggregate base class (`EventStoreAggregate<TenantState>`), 4 lifecycle Handle method signatures with three-outcome patterns (including NoOp for idempotent disable/enable), note that Users/Configuration Apply methods are created here but Handle methods deferred to Epic 3.

3. **Story 2.4 (CommandApi)** — Added 2 new ACs: (a) `AddEventStore()` auto-discovery registration, (b) `/process` endpoint for DAPR service-to-service domain invocation. Added implementation blueprint: `Program.cs` DI pattern, `AssemblyScanner` behavior, 5-layer cascade configuration, `IDomainServiceResolver` mapping, `TenantBootstrapHostedService` detail, `RejectionToHttpStatusMapper` reference. Added DAPR version alignment prerequisite.

### Technical Impact

- **Zero code changes to existing implementation** — Story 2.1 contracts and Epic 1 scaffolding are unaffected
- **One package version update needed** — `Directory.Packages.props`: DAPR 1.16.1 → 1.17.3 (4 package references)
- **No new stories, no scope changes, no timeline impact**

---

## 3. Recommended Approach

**Selected:** Direct Adjustment — Documentation enrichment within existing story boundaries.

**Rationale:**

- All changes are additive enrichments to existing artifacts — no deletions, no rewrites
- Stories 2.2–2.4 haven't been created as implementation artifacts yet — enriching them now (before `create-story`) is the ideal time
- The research provides concrete code patterns that reduce implementation ambiguity and prevent mid-story course corrections
- DAPR version alignment should happen before Story 2.2 to avoid compatibility issues with the EventStore submodule
- No scope change, no new stories, no timeline impact

**Effort:** Low — documentation enrichment + 1 package version bump
**Risk:** Low — additive changes, no code modifications
**Timeline Impact:** None

---

## 4. Detailed Changes Applied

All changes have been applied to the planning artifacts during this workflow:

### Architecture Document Changes

- **D9**: Command Processing Pipeline — detailed end-to-end flow with 5-step actor pipeline and DAPR service-to-service invocation pattern
- **D10**: Aggregate Testing Blueprint — ProcessAsync + CommandEnvelope pattern with code example and test category table
- **DAPR version**: 1.17.0 → 1.17.3 (all references)

### Epics Document Changes

- **Story 2.2**: GlobalAdministratorsState blueprint, aggregate base class, Handle method behaviors, last-admin protection
- **Story 2.3**: TenantState blueprint (9 properties, 9 Apply methods), aggregate base class, 4 lifecycle Handle methods with NoOp patterns
- **Story 2.4**: 2 new ACs (AddEventStore auto-discovery, /process endpoint), Program.cs DI blueprint, DAPR version prerequisite

### No Changes Needed

- PRD: No conflicts — PRD defines requirements, research defines implementation
- Story 2.1 implementation artifact: Contracts are correct per research
- CI/CD pipeline: Compatible
- DAPR component configs: Compatible
- Sprint status: No structural changes

### Pending Housekeeping

- `Directory.Packages.props`: Update DAPR packages from 1.16.1 to 1.17.3 (before Story 2.2 begins)

---

## 5. Implementation Handoff

**Scope:** Minor — No further action required beyond the applied documentation changes.

All architecture and epics enrichments have been applied during this Correct Course workflow. No code changes, no backlog reorganization, no new stories.

**For Story 2.2 implementer (next story):**

- Use `GlobalAdministratorsState` blueprint from epics document
- Extend `EventStoreAggregate<GlobalAdministratorsState>` — framework handles Handle/Apply discovery via reflection
- Test with `ProcessAsync(commandEnvelope, state)` pattern per Architecture §D10
- All 3 Handle methods are `public static` pure functions returning `DomainResult`

**For Story 2.3 implementer:**

- Create `TenantState` with ALL 9 Apply methods (including Users/Configuration) — Handle methods for those are deferred to Epic 3
- Extend `EventStoreAggregate<TenantState>` — same pattern as 2.2
- Implement 4 lifecycle Handle methods with NoOp for idempotent disable/enable
- Same ProcessAsync testing pattern

**For Story 2.4 implementer:**

- Wire `AddEventStore()` / `UseEventStore()` in Program.cs — framework auto-discovers aggregates
- The `/process` endpoint is registered automatically by `UseEventStore()`
- `IDomainServiceResolver` maps aggregates to CommandApi's DAPR AppId
- Implement `TenantBootstrapHostedService` and `RejectionToHttpStatusMapper`
- **Prerequisite:** Update DAPR packages to 1.17.3 in `Directory.Packages.props`

**Success Criteria:**

- Architecture document contains detailed command processing pipeline (verified: D9 section added)
- Architecture document contains aggregate testing blueprint (verified: D10 section added)
- DAPR version references are consistent at 1.17.3 (verified: all references updated)
- Stories 2.2, 2.3, 2.4 contain implementation blueprints (verified: all three enriched)
- Build continues to succeed with zero warnings/errors (verified: no code changes made)
- All 24 contract tests continue to pass (verified: no contract changes made)
