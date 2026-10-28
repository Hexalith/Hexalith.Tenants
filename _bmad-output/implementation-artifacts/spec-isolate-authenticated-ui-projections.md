---
title: 'Isolate authenticated UI projection qualification'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '1436044bf6452bbc5bdb2eaa8b585ab2ce9362ca'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The broad Aspire CI suite fails while seeding an authenticated UI route because its fixture enables background projection workers. The dedicated live-route job masks this by disabling workers globally. Use the existing scoped request-path startup profile for the authenticated UI fixture, preserving the separate worker-enabled delivery qualification and production behavior.

</frozen-after-approval>

## Implementation Notes

- Investigation: CI run 40597845965 passed 498 of 499 Aspire tests; ProtectedRoutesRenderForValidatedCallerAndCurrentTenant timed out after its first membership command committed but its projection did not advance. The REST fixture already scopes WorkersEnabled=false; AuthenticatedTenantsTopologyFixture bypasses that scope.
- Plan: reuse AuthenticatedRouteTopologyFixture.WithRequestPathSettingsAsync at UI startup, assert the actual startup profile in the two seeded UI tests, and extend fixture lifecycle mapping checks. The helper already tests environment restoration after success and failure.
- No intent gaps, irreversible implementation actions, or new public API. Publication and Git synchronization are separately authorized by the user and follow this implementation.
- Validation: build and run focused fixture-isolation tests; run the authenticated UI class and full non-performance Aspire suite without a global worker override. Preserve all CI admission and publication checks.
- Implementation reuses the existing startup helper and checks the runtime profile in both command-seeding UI tests. Lifecycle mapping checks cover both authenticated fixtures.
- Independent review requested unset-environment coverage; parameterized restoration cases now include both null and true values, on success and failure. The broader CI suite remains the regression gate for actual UI startup without the dedicated job override.

## Review Triage Log

- Low, rejected additional helper-wiring test: lifecycle reflection verifies xUnit mapping, not startup execution. The two seeded UI tests assert the captured profile, and the full non-performance CI suite runs them without a global worker override; reverting the startup fix fails those assertions. A second helper extraction solely for a synthetic startup test adds no coverage beyond that gate.
- Low, patched: initially unset worker setting was absent from restoration coverage. Both startup outcomes now verify restoration of absence as well as an existing true setting, including the next child process's environment.

- Validation completed: Debug source-reference integration build passed with zero warnings/errors; all 5 fixture-isolation cases passed without skips. Full hosted qualification is required before the separately authorized release.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Platform`

## Completion Notes List

- The listed root gitlinks declare the complete dependency movement from this record's starting commit through the release source, including independent synchronization commits. The scoped implementation and its validation remain recorded above.
