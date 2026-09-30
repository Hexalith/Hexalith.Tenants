# Story 5.4 re-review verification — 2026-09-30

This run addresses the eight unchecked patches in the 2026-09-30 re-review. Its owning follow-up is `spec-5-4-understand-audit-availability-and-recovery-3.md`, based on canonical revision `55fc6f91283399c50c92a40ee52c3b21ff3e9ea5`. The legacy story's historical baseline and recorded deferrals remain unchanged.

## Build and test commands

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore --no-incremental -p:UseHexalithProjectReferences=true` — passed, zero warnings and errors. The initial restore used the same project/configuration with `--no-restore` omitted.
- Focused mutations use `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -noLogo -parallel none -method '<pattern below>'` after the full non-incremental build above. Each mutation restored the original file bytes in `finally`; the final unmodified build followed all mutations.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md` — passed with no pointer changes. This claim applies to the current follow-up, not the legacy story's unresolved historical baseline.
- `git diff --check` — passed.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore` — pre-review maintained MTP run passed 3,580/3,580, zero failed or skipped, in 41.717 seconds. An intermediate full run timed out in the existing metadata-confirmation test (3,579/3,580); its focused rerun passed, followed by this complete passing run, with no code changes to that flow or test.
- The same maintained MTP command after review passed **3,586/3,586**, zero failed or skipped, in **36.462 seconds**, following a fresh non-incremental Debug source build with zero warnings/errors. All six review findings are addressed in the follow-up triage log; no new deferrals were added.

## Mutation results

| Deliberate regression | Focused method pattern | Result |
| --- | --- | --- |
| Replace `RefreshAsync` and its finalization with the version from `d728ff46` | `*Refresh_failure_after_renderer_teardown_is_non_fatal` | 3/3 failed: each disposed host fault escapes |
| Remove refresh/finalization `when (_disposed)` filters | `*Live_refresh_failure_surfaces_without_counting_and_allows_retry` | 3/3 failed: each live fault is swallowed |
| Remove focus `when (_disposed)` filters | `*Live_focus_handoff_does_not_hide_non_teardown_failures` | 3/3 failed: each live fault is swallowed |
| Move `_refreshInFlight = false` outside the finalization dispatcher callback | `*Dispatcher_finalization_keeps_the_gate_closed_until_the_retry_is_counted` | 1/1 failed: second click invokes the host twice |
| Delete the whole-template `TenantAuditSupportSafety.IsSafe` condition | `*Receipt_component_checks_template_safety_across_summary_segments` | 1/1 failed: unsafe template becomes copyable |
| Remove `role="group"` from `#availability-actions` in the browser fixture | `*Rendered_class_hooks_match_the_390px_browser_fixture` | 1/1 failed: fixture semantics diverge |
| Remove only finalization `when (_disposed)` filters | `*FinalizationContainsDispatcherTeardownButPreservesLiveFailures` | 3/3 failed: live dispatcher failures are swallowed |
| Delete only finalization exception containment | `*FinalizationContainsDispatcherTeardownButPreservesLiveFailures` | 3/3 failed: disposed dispatcher failures escape |
| Corrupt the browser group's accessible name | `*BrowserFixtureRecoveryNameMatchesProductionFrenchResources` | 1/1 failed: accessible name diverges from production French resources |

The summary test retains the `Actor:` label and adds a positive default-template control. A separate cross-segment `b-e-a-r | e-r` fixture isolates the whole-template guard: completed segments are safe individually, while the normalized whole template contains the prohibited fragment. Cancellation tests assert its exception type; async task propagation may recreate a `TaskCanceledException`, so object identity is required only for the other two exception types. All host/dispatcher completion waits are bounded by five seconds.

The last three mutations followed independent review. Six added focused cases passed before mutation: successful host completion after disposal, three separately injected finalization dispatcher failures covering live/disposed lifetimes, actual clear-sort recovery from descending page two, and production-French browser group naming. The original successful-disposal case is retained alongside host-fault coverage. Dispatcher injection restores bUnit's original dispatcher in `finally`, including failure paths.

## Browser evidence

`bash /tmp/story-5-4-browser-debug.sh` passed using Google Chrome `154.0.8037.57`. The temporary script is an exact copy of `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` except that `script_dir` points to the original Browser directory and its four `/obj/Release/` paths use `/obj/Debug/`, so it consumes this run's fresh local Debug CSS. Narrow and desktop rendering, focus handoff, live-region structure, accented availability labels, and full-width narrow recovery stacking passed.

The harness independently rejected its return-true focus mutation, in-flow/hidden removal-dialog CSS mutations, and unstacked availability CSS mutation. These browser checks do not establish the deferred first-announcement behavior in NVDA or VoiceOver.

## Compatibility prerequisite

The existing Fluent UI 5.0.0 update initially prevented compilation: `TenantsWorkspace.razor` used removed sort-event `Column` and `SortByAscending` members, followed by two tests using the removed grid `SortByAscending` property. Local net10.0 API inspection confirmed `DataGridSortEventArgs<T>.SortColumns` and `DataGridSortColumn<T>.Column`/`Ascending`. The narrow fix preserves the primary sort and resets an empty sort to tenant-ID ascending.

The first full maintained MTP run passed all new regressions but failed four old generated-surface assertions (3,576/3,580 passed): Fluent UI now includes the header in `aria-rowcount`. Those expectations now assert two rows for one data row plus its header; existing tenant-column, stale-state, and authorization-clearing assertions remain.

## Commit validation

The repository-pinned `@commitlint/cli@21.2.2` was confirmed with `npm ls @commitlint/cli --depth=0`. `node_modules/.bin/commitlint --edit /tmp/story-5-4-commit-message.txt --verbose` accepted the complete candidate `fix(audit): close recovery review verification gaps`: zero problems and zero warnings.
