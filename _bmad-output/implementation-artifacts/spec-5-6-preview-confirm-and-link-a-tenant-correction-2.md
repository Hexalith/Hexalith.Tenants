---
title: 'Reverify the completed tenant correction flow'
type: 'chore'
created: '2026-10-08'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: fcdcb4205a3f6e46f736cdd3e6f2b20ca2f241df
gitlink_audit_ref: '2b91b05be9c927319fd207b02552d1213997776b'
context:
  - _bmad-output/project-context.md
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.6 is implemented and marked done, but later committed dependency updates have made five target claims in its completion record stale. Its existing verification describes the earlier dependency graph.

**Approach:** Reverify the existing correction behavior through the maintained UI and EventStore command-proof lanes and rendered EN/FR browser harness. Refresh the completion record with exact current dependency revisions and dated evidence while preserving the original story baselines, frozen decisions, historical observations, production behavior, and intentional sprint review status. Keep existing package pins, dependency checkouts, Git indexes and history; create no commits or pushes. Record any validation blocker without weakening its gate.

</frozen-after-approval>

## Implementation Notes

- Read-only investigation found no unmet frozen acceptance criterion. Existing tests cover current-authority rechecks, one retained command attempt, tenant isolation, committed-sequence confirmation, legacy fail-closed behavior, and refusal to link unassociated audit rows. This run changes completion documentation only. The regenerated Epic 5 context was required because canonical architecture is newer than its cache.
- Story key: `5-6-preview-confirm-and-link-a-tenant-correction`. Workflow sprint synchronization makes no change: review is already later than in-progress and equals the final review target.
- Initial `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` failed with CS1704 importing both source and packaged `Hexalith.Commons.UniqueIds`. Restoring the existing source mode with `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:UseNuGetDeps=false -m:1 -v:q` succeeded. Repeating the exact build succeeded with zero warnings/errors. Package pins and source references were not edited.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` passed 3,986 tests, zero failed/skipped, in 1m 05s.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` exited 0 with Chrome 154.0.8037.57. Rendered EN/FR start/preview/recovery, desktop/narrow/forced-colors visibility, exact recovery-reason focus and negative controls passed. This harness covers rendered component markup and static focus helpers; it does not claim authenticated live command egress.
- The original story's gitlink guard initially failed with five `[MISSTATED]` targets. Its original table now explicitly describes pass-14 completion; a dated current record states all eight original-to-current targets and identifies the later pointer-bearing commits. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md` now exits 0. The same command targeting this reverification spec exits 0 with no pointer changes since its own baseline. The original story baselines were preserved.
- From the owning EventStore checkout, `dotnet restore tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -p:UseNuGetDeps=false -m:1 -v:q` and `dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` exited 0; build had zero warnings/errors.
- From EventStore, `dotnet test --project tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore --filter-class '*StateMachineIntegrationTests' --filter-class '*EventDrainRecoveryTests' --filter-class '*AggregateActorIdempotencyTests' --filter-class '*EventPublicationIntegrationTests' --filter-class '*CommandStatusControllerTests'` passed 165 tests, zero failed/skipped. This is the existing five-class command-proof lane, not a full Server suite run.
- Invariant checks confirm unchanged root HEAD, Git index SHA-256, committed gitlinks, original story/EventStore baselines and original frozen-block SHA-256. Both story gitlink guards and root `git diff --check` pass. This run changes the original completion record, required Epic 5 context cache, this reverification spec and its structured evidence report.
- Independent review exposed four omissions in the regenerated context. Corrected it to include the approved exact committed-sequence confirmation, bounded correction expiry, absent-target role-change blocking, and timestamp/intended-role/owner-count preview facts. These corrections preserve approved behavior; no human-owned intent was edited.
- Persistent results and before/after hashes are in `story-5-6-reverification-2026-10-08.json`, SHA-256 `6ec3b0a0803711377d40ac4df2d9cf3929905629af1957f64d92f49b7d5a5705`. The report records observed tool results, full commands, check scope and exact hash extraction. Raw console logs were not retained; the report explicitly distinguishes this summary from raw logs.

## Review Triage Log

- R1 — medium; patch. Generic version advancement in the regenerated context could recreate the proven role-cycle false confirmation. Context now requires exact verified eventful attempt/scope proof, a committed sequence beyond baseline and a fresh matching projection reaching it; original approved intent is unchanged.
- R2 — medium; patch. The context omitted approved correction expiry and could instruct indefinite admission blocking. It now states release with retained recovery identity, fresh-preview replacement and no redispatch of the expired attempt.
- R3 — medium; patch. Membership-only selection omitted the absent role-change target case and could select an unintended restore. The context now distinguishes restore from role-change intent and blocks the latter's absent target.
- R4 — medium; patch. The shortened preview list omitted original timestamp, explicit intended role and owner impact. All three are now explicit in the existing ten-item list.
- R5 — low; patch. Current results lacked a persistent output report. Added a structured report with exact observed commands/outcomes/scopes and a content hash; it truthfully states that raw console logs were not retained.
- R6 — low; patch. Invariant claims lacked their hash values and method. The same report preserves equal before/after root-index/frozen-block hashes and extraction rules, with original baseline and pointer evidence.

All six findings were resolved within the authorized cache/evidence refresh. No findings were deferred. Documentation changes do not require repeating unchanged behavioral lanes.

## File List

- references/Hexalith.Builds
- references/Hexalith.Memories
- references/Hexalith.Platform

## Completion Notes List

The two target rows below describe the original `5bfe0715591ecae8790472b29bb0e7d8986da4ae`
checkpoint only. The dated final historical inventory following them supersedes
their target claims for revalidation; the original run and its evidence stay intact.

- The user's commit `5bfe0715591ecae8790472b29bb0e7d8986da4ae` carried these two pointer moves after the reverification run. The lanes ran at the previous committed gitlinks, and no built input changed.
- references/Hexalith.Memories 3e18d0dcdceb387eff89862c382637da89ad7e47 -> c46973679fcf09bdc5aab19928f2fe03dae82226
- references/Hexalith.Platform eb864b75439371d2bd8fd8047bccb291c699d84c -> 495d1d0dfceb33e6bb628809a42a284526707ff6

**Final historical inventory (2026-10-21, DW-549)**

This audit preserves baseline `fcdcb4205a3f6e46f736cdd3e6f2b20ca2f241df`
and ends at `2b91b05be9c927319fd207b02552d1213997776b`, the final historical
edit of this artifact before this documentation repair. The range includes
independent dependency updates; inventorying them does not attribute them to
the reverification behavior or claim the original lanes ran on their targets.
Later `main` pointer updates are outside this immutable audit.

| Dependency | Preserved baseline | Historical endpoint |
| --- | --- | --- |
| `references/Hexalith.Builds` | `f717a87c26a8266bdde95d18f998ef2ab366d43a` | `a283481c69393dcba911db6a0edcb152167238cc` |
| `references/Hexalith.Memories` | `3e18d0dcdceb387eff89862c382637da89ad7e47` | `dbe4ce0a97c6a3a883af800f7873a99b79434d44` |
| `references/Hexalith.Platform` | `eb864b75439371d2bd8fd8047bccb291c699d84c` | `3a99380e022bb5ba542eb1a191400f8b9e4b913c` |

The complete first-parent pointer landing chain is
`5bfe0715591ecae8790472b29bb0e7d8986da4ae`,
`9ee73062b7e540b69f824803d182f151c96ddb24`, then
`416ab32cca5d4b9d1f56bc5558bc2c974912a8cf`.
The endpoint's later edit adds evidence without moving pointers.

Run the supplied current Markdown against that historical tree:

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md --ref 2b91b05be9c927319fd207b02552d1213997776b
```

Expected: three exact declared targets and `RESULT: PASS`. The frontmatter
`gitlink_audit_ref` selects the same endpoint in changed-story CI for this
pointer-free documentation repair. Running the legacy guard without `--ref`
still compares with moving HEAD and can reject unrelated later dependency
changes. The earlier failures remain valid historical observations.
