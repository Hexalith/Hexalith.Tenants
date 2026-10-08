---
title: 'Reverify tenant correction at the current dependency revisions'
type: 'chore'
created: '2026-10-08'
status: 'done'
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
- The read-only investigation verified tenant-command authority, retained-attempt admission, committed-sequence confirmation, and refusal to pair unassociated tenant audit rows, but missed the audit page's re-enabled global-administrator correction branch. Pass 17 identified that branch as a breach of Story 5.5/5.6's frozen read-only boundary, despite the passing 3,986-test lane. The user chose option (a) on 2026-10-08: restore the read-only gate and its regression test, preserving the standalone panel and complete-evidence loader for Story 5.7. This historical reverification made no source/test changes; its results do not establish compliance with that boundary.
- Both projects restored their existing source mode before Debug serialized builds. Restore/build commands exited 0; both builds reported zero warnings/errors. The complete maintained UI lane passed 3,986 tests with zero failed/skipped. The existing five-class EventStore command-proof lane passed 165 tests with zero failed/skipped. Full commands and raw combined stdout/stderr are preserved in the evidence below.
- The rendered EN/FR browser harness exited 0 with Chrome 154.0.8037.57. Desktop, narrow viewport, forced colors, recovery-reason DOM focus and negative controls passed. This verifies rendered component fixtures and static focus helpers, not an authenticated live application or live command delivery.
- Added an explicitly superseding current-head dependency paragraph to the original Completion Notes, preserving earlier targets as historical records. Original-story and current reverification gitlink guards both exit 0; root `git diff --check` passes. The older reverification spec `-2.md` and its JSON remain historical evidence at their recorded revisions, without a current-head validation claim.
- Persistent results: `story-5-6-current-head-2026-10-08.json`; complete logs, command runner and snapshot helper: `story-5-6-current-head-raw-logs-2026-10-08.zip`. The report hashes the archive and each command log, states exact validation scope and preserves equal before/after root HEAD, root and all nine submodule index hashes, actual submodule HEADs/status, committed gitlinks, original frozen block/baselines and package-configuration hashes. No staging, commits, pushes, dependency updates or initialization occurred. Sprint remains review.
- Independent review prompted two evidence additions: the initial clean-root observation and a final comparison of 613 protected tracked source/test/configuration files to HEAD through Git content filters, plus raw current file SHA-256 values; and evaluated MSBuild source roots/references, SDK 10.0.401, four restored asset hashes and their resolved project/package lists. The initial clean-root observation came from the workflow's tool output; it is not an archived earlier byte snapshot. An initial raw byte comparison encountered Git line-ending normalization; the corrected filtered-blob comparison passes. Archive README identifies capture scripts as historical workspace records and states the serial execution order. They are not maintained portable validation tools.

## Review Triage Log

- R1 — low; patch. Index/HEAD equality alone does not demonstrate unchanged unstaged source. Added the initial clean-tree tool observation and a final protected-content record: all 613 normalized Git blob identities match HEAD, with current raw SHA-256 values. The record explicitly distinguishes this comparison from an earlier raw byte snapshot.
- R2 — medium; patch. Source-root overrides could make checkout-only provenance insufficient to identify which dependencies the tested projects consumed. Added evaluated root/reference paths for both owning test projects, SDK version, four asset hashes, and resolved project/package lists after the successful lanes with unchanged configuration/checkouts.
- R3 — low; reject. Archived capture scripts retain the absolute historical workspace path. They are execution records, not a portable replay tool; normal review reads logs and exact commands. Adding a root-argument interface for rare archive reuse is beyond a direct record correction. README now makes their purpose and fresh-directory repeat procedure explicit.
- R4 — low; reject. Reusing the runner directory could overwrite earlier logs while appending duplicate records. This run executed each behavioral lane once; the retained manifest and log hashes have one matching record per required command. Supporting historical reruns in that capture helper adds bookkeeping for uncommon archive reuse; repeat verification uses a fresh directory as documented.
- R5 — low; reject. Concurrent reuse could lose manifest rows because the archival runner does not lock writes. This run serialized UI and EventStore lanes, and the complete required command set is present. Adding interprocess locking for a non-maintained capture record adds complexity outside ordinary evidence review; README states the serial order.
- R6 — low; reject. Reusing finalization after failed/incomplete lanes could mislabel its command count. This archive has the complete required command set, all final commands exited 0, builds have zero warnings/errors, and both test logs have zero failed/skipped tests. Those outcomes and hashes were checked directly before completion. Making the historical helper a reusable guarded verifier adds new behavior for an uncommon replay path.

Both evidence omissions were corrected; four archive-reuse findings were rejected as low under the one-shot review rule. No findings were deferred. Final documentation/evidence checks verify archive/report hashes. Behavioral lanes are repeated when changed dependency source introduces new validation concerns, as recorded below.

## File List

- references/Hexalith.EventStore
- references/Hexalith.McpCli
- references/Hexalith.Memories
- references/Hexalith.Platform

## Completion Notes List

- Concurrent external root commit `57ee33ef2511eebdf15a401dc19787423696039e` committed the first verification artifacts and these four dependency advances. This workflow did not create or push that commit or update its checkouts. The original baseline is preserved. First-pass evidence at root `032573384d3df4bc7a5bc0e69945ecca4e00967f` remains the historical committed report/archive; later closeout evidence is recorded separately.
- references/Hexalith.EventStore 8dd7dc2ecdb2c06ecb900676042aa42b66619ee0 -> 07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93
- references/Hexalith.McpCli e159f82b7528797fc245045625ff387d65294ba9 -> 342e072231279831c2fca0a85cbcd9205444fbec
- references/Hexalith.Memories 8c7c1e9621ecae31150f0e6ae65f3ce6ca81cbed -> 906bc07ad6a8e4912a7222d9d097da434148266a
- references/Hexalith.Platform d103ab8cdd3e523ca42ded8135cf7fdb0eeb792b -> f5a0d72f9b72e88008562147a7085da0607d55f7
- The final evidence capture initially stopped because the original-story gitlink guard detected the four concurrent moves. That failure prevented packaging stale current-head claims. The original Completion Notes now explicitly reconcile those targets. Because EventStore executable source changed, the maintained behavioral lanes are rerun at the new checkouts rather than treating first-pass results as current. Closeout preservation checks use a new measured root/index/checkout baseline and include a before/after raw-content fingerprint for 613 protected root files.
- Fresh closeout at root `57ee33ef2511eebdf15a401dc19787423696039e`: both source-mode restore/build lanes exit 0 with zero warnings/errors; complete UI tests pass 3,986, and the five-class EventStore command-proof lane passes 165, with zero failed/skipped. The rendered EN/FR browser harness passes with its existing fixture/static-helper scope. Evaluated source roots/references, four restored asset hashes and resolved package/project lists, SDK 10.0.401, original frozen text/baselines and equal closeout root/submodule HEAD/index/package/content fingerprints are preserved in `story-5-6-closeout-2026-10-08.json` and `story-5-6-closeout-raw-logs-2026-10-08.zip`. These separate artifacts complete the review evidence additions without overwriting the committed first-pass report/archive. Both current story guards and `git diff --check` pass. This workflow performs no Git mutation; sprint remains review. Pass 17 subsequently found that unchanged frozen text and passing tests did not prove the global-administrator read-only acceptance boundary: this closeout included the re-enabled branch and an inverted guard test. The dated option-(a) remediation is owned by the original Story 5.6 spec, not these historical archive bytes.
