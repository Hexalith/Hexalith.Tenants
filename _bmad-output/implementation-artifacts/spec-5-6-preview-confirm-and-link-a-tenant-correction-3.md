---
title: 'Reverify tenant correction at the current dependency revisions'
type: 'chore'
created: '2026-10-08'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 032573384d3df4bc7a5bc0e69945ecca4e00967f
context:
  - _bmad-output/project-context.md
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.6 is implemented, but five later dependency advances have invalidated its recorded current targets and gitlink guard. Verification must describe the actual checkouts used now.

**Approach:** Reverify the existing tenant correction through the complete UI test project, the maintained five-class EventStore command-proof lane, and the rendered EN/FR browser harness. Add a dated current-revision record and persistent raw results while preserving original acceptance, baselines and historical evidence. Keep production code, tests, package pins, dependency checkouts, Git indexes and history unchanged. Preserve the intentional sprint review status; create no commits or pushes. Record exact blockers and validation scope without weakening gates or claiming authenticated live-command coverage.

</frozen-after-approval>

## Implementation Notes

- Planning facts: no unresolved user-visible choice; no irreversible work; the footprint is verification documentation, structured evidence and a raw-log archive. The existing correction surfaces and checks are reused. The Epic 5 cache is valid and is not regenerated.
- Story key: `5-6-preview-confirm-and-link-a-tenant-correction`; sprint is already review, later than in-progress, so synchronization requires no edit.
- Initial original-story gitlink guard exited 1 with five MISSTATED targets: Builds, Commons, EventStore, Memories and Platform. All nine actual root-declared submodule HEADs equal their committed gitlinks and have clean working trees. Original frozen-block SHA-256 is `f4cc2b4f01d2bc0e3426ff40040c8c5d14c5416dc91cb6d709cc5c68a9e4c866`.
- Read-only code investigation found no demonstrated unmet frozen acceptance criterion. Current authority is rechecked before dispatch; the retained attempt and aggregate lease prevent duplicate logical commands; confirmation requires this verified command's committed sequence. Audit DTOs still lack deterministic command association, so the approved missing-support state refuses paired links rather than attributing a coincidental event. No source or test changes were needed.
- Both projects restored their existing source mode before Debug serialized builds. Restore/build commands exited 0; both builds reported zero warnings/errors. The complete maintained UI lane passed 3,986 tests with zero failed/skipped. The existing five-class EventStore command-proof lane passed 165 tests with zero failed/skipped. Full commands and raw combined stdout/stderr are preserved in the evidence below.
- The rendered EN/FR browser harness exited 0 with Chrome 154.0.8037.57. Desktop, narrow viewport, forced colors, recovery-reason DOM focus and negative controls passed. This verifies rendered component fixtures and static focus helpers, not an authenticated live application or live command delivery.
- Added an explicitly superseding current-head dependency paragraph to the original Completion Notes, preserving earlier targets as historical records. Original-story and current reverification gitlink guards both exit 0; root `git diff --check` passes. The older reverification spec `-2.md` and its JSON remain historical evidence at their recorded revisions, without a current-head validation claim.
- Persistent results: `story-5-6-current-head-2026-10-08.json`; complete logs, command runner and snapshot helper: `story-5-6-current-head-raw-logs-2026-10-08.zip`. The report hashes the archive and each command log, states exact validation scope and preserves equal before/after root HEAD, root and all nine submodule index hashes, actual submodule HEADs/status, committed gitlinks, original frozen block/baselines and package-configuration hashes. No staging, commits, pushes, dependency updates or initialization occurred. Sprint remains review.
