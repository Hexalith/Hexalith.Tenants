# Sprint Change Proposal — Rejection Events Pattern

**Date:** 2026-03-13
**Author:** Jerome (via Correct Course workflow)
**Change Scope:** Minor (additive changes within existing stories)
**Status:** APPROVED

---

## 1. Issue Summary

**Trigger:** Pre-implementation compliance audit comparing Hexalith.Tenants planning artifacts against actual Hexalith.EventStore source code and the technical research document.

**Finding:** The architecture document specified domain exception classes (e.g., `TenantNotFoundException`) for business rule violations in aggregate Handle methods. However, the EventStore framework's `DomainResult` provides a three-outcome model (Success/Rejection/NoOp) with `IRejectionEvent` types that offer superior behavior:

- Rejection events are persisted in the idempotency store (exceptions are not)
- Rejection events provide an audit trail of failed command attempts
- Handle methods remain pure functions (no thrown exceptions = no side effects)
- Duplicate commands hitting a business rule get the cached rejection (instead of re-executing and re-throwing)

**Evidence:** Examined `EventStoreAggregate.DispatchCommandAsync()` in source code — confirmed no try-catch around Handle invocations. Thrown exceptions propagate as unhandled failures, bypassing idempotency recording.

---

## 2. Impact Analysis

**Epic Impact:**
- No epic-level changes. All 8 epics remain as planned.

**Story Impact:**
- Story 2.1 (Tenant Domain Contracts): +8 rejection event records, +1 AC, +1 task
- Story 2.2 (Global Administrator Aggregate): Handle methods use `DomainResult.Rejection()` instead of `throw`
- Story 2.3 (Tenant Aggregate Lifecycle): Handle methods use `DomainResult.Rejection()` instead of `throw`
- Story 3.1 (User-Role Management): Handle methods use `DomainResult.Rejection()` instead of `throw`
- Story 3.3 (Configuration Management): Handle methods use `DomainResult.Rejection()` instead of `throw`

**Artifact Conflicts Resolved:**
- Architecture document: Updated error handling decision, Handle method examples, naming conventions, file structure, enforcement guidelines
- Epics document: All 11 exception references renamed to rejection event equivalents
- PRD: 2 narrative references updated for consistency
- Story 2.1: Added rejection event types, tests, anti-patterns

**Technical Impact:**
- Domain exception classes are NOT created (7 exception classes removed from plan)
- 8 rejection event records added to Contracts (public API surface)
- Handle methods consistently return `DomainResult` (pure functions)
- CommandApi needs `RejectionToHttpStatusMapper` to convert rejection events to HTTP status codes

---

## 3. Recommended Approach

**Selected:** Direct Adjustment — Add rejection event types within existing story structure.

**Rationale:**
- Additive change (no existing code to modify)
- Aligns with EventStore's designed three-outcome model
- Enables idempotency caching of rejections
- Makes Handle methods truly pure (testable, predictable)
- Small scope: 8 record types in Contracts, pattern change in Handle methods

**Effort:** Low — 8 simple record types + pattern change in Handle method implementations
**Risk:** Low — No breaking changes, no scope change, no timeline impact
**Timeline Impact:** None

---

## 4. Detailed Changes Applied

### Story 2.1 (Tenant Domain Contracts)
- Added AC #8 (rejection events) and AC #9 (serialization round-trip)
- Added Task 13 (8 rejection event subtasks)
- Added `Events/Rejections/` folder to file structure
- Added rejection event template and naming conventions to dev notes
- Updated naming convention tests to cover rejection events
- Added anti-pattern: "DO NOT create domain exception classes"

### Architecture Document
- Updated error handling decision: rejection events via `DomainResult.Rejection()` instead of domain exceptions
- Updated Handle method examples with three-outcome pattern (Success/Rejection/NoOp)
- Updated rejection event naming convention (replaces exception naming)
- Removed Exceptions/ folder from Server project structure
- Added Rejections/ folder to Contracts project structure
- Updated type location rules: rejection events in Contracts
- Updated enforcement guidelines: "NEVER throw domain exceptions from Handle methods"
- Updated HTTP status code mapping to reference rejection event types
- Updated bootstrap multi-instance behavior description

### Epics Document
- Renamed all 11 exception references to rejection event equivalents

### PRD
- Updated 2 narrative references for consistency

---

## 5. Implementation Handoff

**Scope:** Minor — Direct implementation by development team.

**No backlog reorganization needed.** All changes are within existing story boundaries.

**Next steps:**
1. Implement Story 2.1 (Tenant Domain Contracts) — now includes rejection events
2. Implement Stories 2.2/2.3 using `DomainResult.Rejection()` pattern in Handle methods
3. When implementing CommandApi (Story 2.4), add `RejectionToHttpStatusMapper` middleware

**Success criteria:**
- All 8 rejection event records compile and pass serialization round-trip tests
- Handle methods consistently return `DomainResult` (Success/Rejection/NoOp)
- No domain exception classes exist in the codebase
- Naming convention tests pass for rejection events (`*Rejection` suffix)
