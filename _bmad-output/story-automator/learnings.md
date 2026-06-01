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
