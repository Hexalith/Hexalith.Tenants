# Story Automator Learnings

## Run: 2026-06-01T04:06:57Z

**Epic:** Tenants - Epic Breakdown
**Stories:** 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6

### Patterns Observed
- Exact current story files and sprint-status keys were more reliable than broad artifact searches because older same-number story artifacts remained in the repository.
- Direct xUnit execution was the reliable validation path when VSTest built successfully but could not open its listener in the sandbox.
- Epic retrospectives were useful for finding documentation drift after implementation, especially around resource names, auth defaults, and sample behavior.

### Code Review Insights
- Common issues: stale File List metadata, stale documentation terminology, payload-safe logging, fail-closed validation, and tenant/envelope consistency checks.
- Most stories reached done after one review cycle; Story 2.3 required a second review cycle.

### Timing Estimates
- create-story: minutes per story once artifact collisions were handled.
- dev-story: longer for EventStore boundary changes, projection behavior, and query/security stories.
- code-review: usually one focused cycle per story, with auto-fixes applied before finalization.

### Recommendations for Future Runs
- Keep historical story artifact collisions documented and prefer exact story key verification in automation.
- Preserve direct xUnit fallback commands in story evidence whenever the local test runner environment blocks VSTest.
- Re-check user-facing docs after topology, auth, event, projection, or sample logging changes.
- Keep consumer-local Client projection behavior separate from Tenants server-owned query projection design in Epic 5.

## Run: 2026-06-01T13:02:43Z

**Epic:** Tenants - Epic Breakdown
**Stories:** 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 6.1, 6.2, 6.3, 6.4

### Patterns Observed
- Historical story artifacts still require exact current sprint-status key checks before create-story, especially when story numbers were reused with new slugs.
- The direct xUnit v3 executable remained the reliable test path when `dotnet test`/VSTest hit local socket restrictions.
- Retrospectives were useful closure points for marking epic-level sprint rows done and verifying documentation drift against implementation.

### Code Review Insights
- Common issues: stale File List metadata, safe diagnostic output, explicit conformance coverage, tenant/envelope identity handling, and clear testing responsibility boundaries.
- Most stories reached done in one review cycle; several reviews applied small auto-fixes before sprint-status moved to done.

### Timing Estimates
- create-story: a few minutes per story after artifact collisions were handled.
- dev-story: longest for projection/query authorization, conformance coverage, and Testing helper stories.
- code-review: usually one cycle, with direct test-runner fallback evidence needed in sandboxed validation.

### Recommendations for Future Runs
- Keep exact story-key verification and archive stale story artifacts before creating replacements.
- Preserve direct xUnit fallback commands in story records whenever VSTest cannot open its listener.
- Treat Testing package evidence as aggregate/fake parity evidence only; hosted runtime confidence belongs to Epic 7.
- Make File List reconciliation an explicit review gate for future stories.
