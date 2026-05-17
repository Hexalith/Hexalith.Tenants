# Post-Readiness 2026-05-16: Implementation Readiness Grooming

Status: done

## Story

As a product and engineering team,
I want the May 16 readiness risks resolved in planning artifacts before additional implementation,
so that upcoming stories have explicit product policy, dependency sequencing, and Phase 2 scope boundaries.

## Acceptance Criteria

1. Given Story 9.3 is selected for implementation, when disabled-tenant and orphan-membership query behavior is reviewed, then the story carries an explicit policy decision before runtime behavior changes.
2. Given Story 10.3 is selected for implementation, when cancellation-token support depends on EventStore projection APIs, then the EventStore prerequisite is split out as Story 10.3A and Tenants integration remains Story 10.3B.
3. Given Story 2.4 is reviewed, when its completed evidence is assessed, then the story is treated as five logical work packages for review and future maintenance rather than a model for future sprint sizing.
4. Given Epic 12 is reviewed, when Phase 1 scope is assessed, then the epic is clearly Phase 2 planning/readiness and not shipped Admin UI behavior.
5. Given the sprint record is inspected, when this correction is complete, then the story status remains `done` and the artifact exists.

## Tasks / Subtasks

- [x] Task 1: Capture the approved readiness grooming.
  - [x] Record the direct-adjustment decision in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16.md`.
  - [x] Confirm the correction does not alter PRD scope, architecture policy, or Phase 1 MVP boundaries.
- [x] Task 2: Apply planning changes.
  - [x] Settle Story 9.3 disabled-tenant and orphan-membership query policy.
  - [x] Split Story 10.3 into EventStore prerequisite work and Tenants integration work.
  - [x] Record Story 2.4 logical packages for review and future maintenance.
  - [x] Clarify Epic 12 as Phase 2 planning/readiness scope.
- [x] Task 3: Preserve tracking evidence.
  - [x] Keep the correction marked `done` in `sprint-status.yaml`.
  - [x] Recreate this missing artifact so pre-development hardening can verify status-artifact consistency.

## Dev Notes

- Triggering source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-16.md`.
- Approved correction: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16.md`.
- The correction protects implementation sequencing: Story 9.3 now has policy, Story 10.3B is blocked by EventStore API readiness, Story 2.4 is not a future sizing model, and Epic 12 remains Phase 2 planning/readiness.
- This artifact was reconstructed on 2026-05-17 because the sprint status had a `done` entry but no matching story artifact. The reconstruction preserves the approved correction and tracking evidence; it does not assert a recovered original Dev Agent Record.

## Dev Agent Record

### Completion Notes List

- Story 9.3 now states disabled tenants remain visible to authorized callers with status included, orphan memberships are filtered from normal user-facing query results, and eventual consistency after membership removal is accepted and documented.
- Story 10.3 is split into EventStore prerequisite work and Tenants integration work, with Tenants blocked until the EventStore dependency is resolved or a compatible API is named.
- Story 2.4 has review packages for command API/event processing, bootstrap idempotency, DAPR pub/sub recovery, API error/auth mapping, and Tier 2 verification.
- Epic 12 is marked as Phase 2 planning/readiness rather than Phase 1 product implementation.

### File List

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16.md` - approval and detailed implementation-readiness grooming proposal.
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-16.md` - triggering readiness assessment.
- `_bmad-output/planning-artifacts/epics.md` - groomed story and epic planning content.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - tracking entry.

## Change Log

- **2026-05-16**: Applied approved implementation-readiness grooming.
- **2026-05-17**: Reconstructed missing completed-story artifact to repair status-artifact consistency.
