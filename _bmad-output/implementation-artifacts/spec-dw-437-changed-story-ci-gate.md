---
title: 'Enforce gitlink declarations for changed stories in CI'
type: 'chore'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: 'fc2d4821e002ea56eaed79b2acae4cc0bfc8ef10'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-437 and duplicate findings DW-451/DW-534 record that Story Guards tests the validator but never validates changed story artifacts. Undeclared dependency movements can therefore reach main while its synthetic regression lane passes.

**Approach:** Run the existing production validator against changed story snapshots in push and pull-request ranges, with full history and canonical comparison revisions. Keep its baseline and owned-commit checks. Allow deliberate historical audits only when a canonical recorded endpoint predates the event and no gitlink commit occurs anywhere in the event range; fail closed on missing or invalid evidence. Verify actual Git histories and refusal controls, close the three qualified records, preserve historical findings and unrelated work, and commit/push the finished change as requested.

</frozen-after-approval>

## Implementation Notes

- Entry: clean main at `fc2d4821e002ea56eaed79b2acae4cc0bfc8ef10`. Selected the first maintenance row, DW-437/DW-451/DW-534. DW-431 already repaired Story 5.2 history; DW-198 and unrelated historical attribution records remain separate.
- Investigation: `.github/workflows/story-guards.yml::gitlink-guard` currently uses a shallow checkout and three synthetic lanes. `scripts/validate-story-gitlinks.py` reads its supplied Markdown but uses `--ref` for the Git tree; preserve that API and pass full endpoint IDs, never literal HEAD. Temporary snapshots must preserve each story basename for its baseline heuristic. No existing machine-readable historical endpoint field exists.
- Footprint: one stdlib runner, one real-Git regression lane, Story Guards wiring, concise tooling documentation, this spec, validation evidence, selected ledger records and one maintenance row. No application API, dependency, persistence, UI or Aspire changes. No unresolved intent gap; implementation is reversible and publication is explicitly authorized.
- Runner decisions: use GitHub's event JSON, never shell interpolation of event values. Push uses before..after; PR uses merge-base(base, head)..head, excluding unrelated base-branch changes and GitHub's synthetic merge commit. Reject unavailable/zero/noncanonical commits and nonancestor push ranges instead of silently skipping them.
- Select Markdown in implementation artifacts by conventional spec/numeric story filenames or baseline metadata in either old/new snapshot. Detect renamed/removed metadata rather than silently dropping it. Ordinary evidence documents are excluded. Use NUL-delimited Git output, preserve spaces, aggregate all selected failures, and inspect committed snapshots rather than a dirty worktree.
- `gitlink_audit_ref` is an optional full canonical historical endpoint. Require it at or before event base, require the story to exist there with the same baseline, and reject it if any event commit moves a references gitlink, even if later reverted. No automatic endpoint guessing or sweeping unchanged historical stories. A removed story is allowed only in a pointer-free event range; pointer-bearing deletions fail closed.
- Validate the actual runner through disposable repositories for push/PR selection, later bundled bumps, declared and owned-chain controls, historical repair and pointer/reversion refusals, malformed/missing metadata, rename/space paths, removed stories, independent base advances, dirty-worktree isolation and continued checking after failure. Run all retained Python lanes, actionlint, whitespace, own gitlink guard, exact ledger/index preservation and pinned commitlint.
- Initial qualification passes 33 new cases and retained 77/13/31 lanes, zero failures/skips. Three isolated faulty runner variants are rejected with 10/3/1 assertion failures and zero errors. Actionlint, Python syntax, own-story guard, whitespace and exact ledger/index preservation pass. A disposable committed replay of all eight task files selects exactly the new spec and passes the production runner.
- Closed only DW-437/DW-451/DW-534 with pre-change fields retained; 656 IDs, 394 done/262 open and 236 exact-covering maintenance rows. DW-198, archive/sprint/release, dependency pointers and unrelated records remain preserved. Both task and fixture message candidates pass pinned commitlint 21.2.3. Read-only origin fetch confirms entry remains current. Follow the tracked project context's feature-branch rule: task branch is `fix/dw-437-changed-story-gitlink-gate`.
- Independent review identified nine corrections; all were patched within the runner/workflow footprint and qualified with direct reproductions. The runner now selects stories across every event commit, preserves rename identities and literal paths, refuses baseline narrowing and erased identities, and binds historical endpoints across prior renames. A final follow-up control closes the historical rename-back gap. The maintained runner lane passes 49/49. Historical notes above describe the initial implementation; the stricter final rules and all review dispositions below supersede its deletion allowance and final-tree-only selection.
- Independent final correction recheck found no remaining concrete issue, passing 49/49 and 13 targeted controls plus merged-history rename/revert controls. Final isolated mutations fail 11/3/2 assertions across 49 cases each, zero errors. Combined maintained evidence is 170 passing tests. Actionlint, own-story guard, whitespace and pinned commitlint pass. No review deferral remains.

## Review Triage Log

- High / patch: a replacement baseline could hide an event pointer commit. Require baseline at/before event base and permit only ancestor corrections to existing baselines; direct event/pre-event narrowing controls fail, while a broadening correction passes.
- High / patch: net final-tree selection missed restored or create/delete stories. Collect identities from each event commit, including merge first-parent diffs; restored Markdown still receives the production audit and disappeared stories refuse.
- High / patch: a new basename hid the original mid-story baseline. Preserve all observed basenames and add exact literal-path baseline inspection, retaining the production guard's owned-mode semantics. Ordinary and CR-path baseline controls fail as required.
- High / patch: universal newline decoding lost CR filenames and silently selected zero stories. Decode Git stdout bytes explicitly and use literal pathspecs; CR/LF/glob filename controls all select and reject their undeclared bumps.
- High / patch: an initial historical snapshot could conceal later pointer-bearing story completion. Follow historical story identities and refuse later associated pointer commits; retain matching baseline and pointer-free current event restrictions. Valid unrelated-later-pointer historical repair still passes.
- Medium / patch: retargeting an existing PR to main did not trigger default PR activity types. Add edited; a retargeted-base event test proves the newly selected range is checked. Verified the activity contract against GitHub's official documentation.
- Low / patch: pushall lanes were skipped after artifact failure. Give all four regression lanes the same non-cancelled condition; actionlint validates the final workflow.
- Low / patch: strict file-mode checks rejected ordinary evidence symlinks before classification. Classify committed blob text without following targets, then enforce regular mode only for selected story identities. Ordinary evidence is excluded; story type changes still refuse.
- Medium / patch: rewritten renames below Git's similarity threshold could discard baseline metadata. Refuse erased story identities even in pointer-free events; the ambiguous replacement control fails instead of guessing ownership.
- High / patch from correction recheck: historical rename-away/rename-back could hide an intermediate story pointer commit. Follow historical rename lineage, preserve endpoint names and refuse post-endpoint recreation. Direct negative controls and valid historical-after-rename acceptance pass.
- Low / patch from correction recheck: the stricter exact-path baseline refusal changed one expected diagnostic. Corrected the assertion to the actual mid-story-baseline refusal; no production refusal was weakened. Intermediate historical-follow runs exposed blank patch lines from Git's diff-merges option; explicit no-patch output and split commit IDs correct transport, with all final cases passing.
