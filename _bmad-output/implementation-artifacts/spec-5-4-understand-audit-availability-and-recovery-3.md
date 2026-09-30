---
title: 'Close Story 5.4 re-review verification gaps'
type: 'bugfix'
created: '2026-09-30'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 55fc6f91283399c50c92a40ee52c3b21ff3e9ea5
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4's re-review found eight remaining patches: refresh teardown, live exceptions, and dispatcher finalization have tests that survive regressions; the unsafe summary fixture fails for another reason; the French recovery group name loses accents; and browser-group parity and empty-group suppression are unproven.

**Approach:** Close exactly the eight unchecked `[Review][Patch]` findings in the legacy story. Add bounded, deterministic regression tests and mutation evidence for refresh teardown and live failures, atomic dispatcher bookkeeping, and isolated localized-template safety. Restore the French receipt group name and browser fixture semantics, and assert Loading has no recovery group. Preserve audit derivation, receipt authority, production refresh behavior, and all recorded deferrals.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent choices or irreversible operations. Expected footprint: the availability and receipt component tests, browser availability fixture, French resources, and workflow tracking artifacts. No new public API or dependency changes.
- Use the existing reflection helpers for focused lifetime tests; canceled tasks must be asserted directly because the renderer treats event cancellation as non-fatal. Fault the actual pending host task after disposal and bound every asynchronous completion.
- The default Ready direct receipt has complete safe fields; the unsafe template must retain `Actor:` so field-label validation cannot mask template safety. Add a positive copy control before mutation verification.
- The already-running Aspire AppHost was inspected before changes: all 20 resources were Running and Healthy. Story key: `5-4-understand-audit-availability-and-recovery`; existing in-progress sprint status needs no initial update.
- The first Debug source build exposed an existing Fluent UI 5.0.0 compatibility failure in `TenantsWorkspace.razor`: CS1061 for removed sort-event `Column` and `SortByAscending` members. The required narrow prerequisite reads the primary entry of `SortColumns` and its `Ascending` flag; an empty sort retains tenant-ID ascending. Local net10.0 API inspection confirmed the new members. No package pins change.
- The unsafe `Actor: Bearer` template also fails completed-segment safety, so an additional cross-segment `b-e-a-r | e-r` template isolates whole-template safety from the independent completed-segment check.
- The compatibility prerequisite also updates two grid sort-state assertions to `SortColumns` and four generated-surface ARIA count assertions to include the header row. The initial maintained MTP run passed all new tests but failed these four legacy expectations (3,576/3,580 passed); no runtime data or access behavior changed.
- Mutation verification rejected all six deliberate regressions: old refresh implementation (3 failures), unconditional refresh catches (3), unconditional focus catches (3), off-dispatcher gate reset (1), removed whole-template safety check (1), and missing browser fixture group role (1). Original file bytes were restored after every mutation, followed by a fresh non-incremental Debug source build.
- The Chrome 154 browser harness passed at narrow and desktop widths, including its return-true, removal-dialog layout/visibility, and unstacked-availability mutations. A temporary copy points to the freshly built Debug scoped CSS and retains the original Browser directory for all other inputs, honoring the local Debug build requirement.
- Final maintained MTP validation passed 3,580/3,580 with no skipped tests after a zero-warning/error non-incremental Debug source build. An intermediate existing metadata-proof test timeout passed on its focused rerun and the subsequent full run without changes. Diff hygiene and the current follow-up gitlink guard passed. Exact commands and mutation results are retained in `story-5-4-re-review-verification-2026-09-30.md`.
- Independent review prompted six additional passing focused cases: restore successful completion after disposal; separately inject each of the three dispatcher failures into finalization and distinguish live from disposed lifetime; verify clear-sort recovery through the real grid callback, canonical URL, and cursor reset; assert selected deep-link columns; and match the browser group's accessible name against a component using production French resources. The faulting dispatcher replaces bUnit's dispatcher field only inside a `try/finally` and restores it before context cleanup.
- Final post-review validation passed the non-incremental Debug source build with zero warnings/errors and maintained MTP with 3,586/3,586 tests, zero failed or skipped (36.462 seconds). Three further mutations separately removed finalization filters, removed finalization containment, and corrupted the fixture group name; all were rejected. Nine code/fixture mutations in total were rejected, with original sources restored before the final build. All six independent review findings are addressed; no new deferrals were added. Story and sprint tracking move to review.

## Review Triage Log

- `low` — the listed verification artifact was initially absent during review; patched by adding the report with exact commands, mutation outcomes, baseline scope, browser adaptation, and commitlint evidence.
- `low` — the eight legacy checklist entries were initially unchecked during review; patched after verification by marking all eight complete and adding current completion/debug references.
- `medium` — replacing the original teardown test removed successful-completion coverage and left dispatcher finalization catches independently unproven; patched with the bounded successful-completion test and three dispatcher-fault cases covering both live and disposed lifetimes. All four focused cases pass.
- `low` — the new empty-sort compatibility path lacked behavioral coverage; patched with a real grid clear-sort test from descending page two that asserts tenant-ID ascending, a canonical URL, and reset cursor/history. The focused case passes.
- `low` — sort restoration asserted direction without selected column; patched by also asserting `tenant-id` for the name deep link and `tenant-status` after status navigation.
- `low` — browser fixture parity checked group role without its accessible name; patched with a passing comparison to the production French localizer and a rendered recovery group. No new review findings were deferred; existing story deferrals remain.
