# EventStore historical gitlink reconciliation — 2026-10-29

This supplements the preserved EventStore 3.128.4 verification; its earlier
failed gitlink observations remain truthful pre-repair results. Package restore,
build and 10,887 passing tests are not rerun or relabelled here. This repair
changes audit records only, with the production validators and dependencies
unchanged.

The original story start is `1846c7128cf0f6b16f9e62f38032bcab78e26e38`, as recorded
in creation `1dffe336c9fde823b7b8ed6df9d63019dae64b90`. October 10 continuation
`ef4b7f2f99b2a60910b94481a198f591c51bfe5c` changed the baseline to
`9e9e04308ea1060010a64bb8b800e755c4aaf99d`. That narrowing excluded the creation's
four pointer changes. The spec now restores its original start, retaining both
values and all prior observations as history. It never substitutes a newer HEAD.

The retired `gitlink_audit_ref` was
`ef4b7f2f99b2a60910b94481a198f591c51bfe5c`. The default direct validator never
reads that field. The CI runner honors it only with the same baseline as its
committed endpoint snapshot. The historical PASS using the narrowed baseline
therefore did not prove coverage of the original creation. Removing this active
field makes CI audit the entire original-baseline-to-event-head range. It avoids
inventing an endpoint exception or weakening the validator. The former value
remains in this evidence and the spec's historical notes.

The current inventory ends at reconciliation entry
`73478ea3b21dbdc416d5b54b59260f851fbcd965`. Nine explicit declarations and exact
target SHAs cover all current net pointer differences, including the eight
previously undeclared paths. These are historical **range contents**, not claims
that independent dependency work belongs to this story. A future changed target
must be reconciled explicitly; it will not silently pass this inventory.

The [manifest](eventstore-gitlink-reconciliation-2026-10-29.json) retains every
first-parent pointer-bearing landing (86 commits, 258 moves), its complete
changed-file list and exact before/after Git objects. First-parent landings
include merge results without counting merged branch transitions twice. The
verifier separately checks full-history story artifact selection. Exact story
and versioned-evidence paths establish association; common ledger/context files
and commit subjects do not establish ownership or behavioral necessity.

| Story-associated landing | Pointer count | Attribution |
| --- | --- | --- |
| `1dffe336c9fde823b7b8ed6df9d63019dae64b90` | 4 | Mixed creation commit carries this spec/evidence plus Builds, EventStore, Memories and Platform. This does not prove those dependency moves were all required by the story or performed by its implementing assistant. |
| `ef4b7f2f99b2a60910b94481a198f591c51bfe5c` | 0 | October 10 continuation documentation and evidence. |
| `2cd3cc270e88d3cdbb47a1a938fd6a0951b6f0d4` | 0 | October 21 wording clarification. |

All other pointer landings are independent range contents with no changes to
these named story artifacts. No `Story-Owned Gitlink Commits` section claims
them. The creation's committed Builds target is
`6f07763bd955d22ace0123798add528dc933bf51`; the older implementation observation
of an external checkout at `2169b866912786536c6e338440b8436e5b826c88` remains a
separate historical fact, not a substitute for that committed target. The
manifest includes four original-creation targets, six original-to-retired-endpoint
targets and all nine original-to-entry targets.

Reproduce the relevant checks from the repository root:

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md
python3 _bmad-output/implementation-artifacts/eventstore-gitlink-reconciliation-verify.py
python3 tests/scripts/test_validate_story_gitlinks.py
python3 tests/scripts/test_validate_changed_story_gitlinks.py
git diff --check
```

The verifier binds the fixed reconciliation entry for repeatability. Both
production validators must match their entry Git blobs before import. It checks
the original baseline directly from the creation snapshot, complete raw pointer
history, full-history story association, all nine exact targets and declarations,
the CI current-range/baseline-broadening contract, frozen-intent preservation and
unchanged versioned package-evidence hashes, with seven committed artifacts
anchored to Git and the two uncommitted 3.128.4 artifacts bound to the entry
snapshot. It requires the exact artifact/filter inventories, Git subjects and
qualified attribution categories. Its 23 snapshot/control checks
comprise one passing current document, nine rejected omissions, nine rejected
wrong targets, two passing generated historical-window controls and two archived pre-repair document replays. Those two
controls use each historical tree's actual inventory; they do not claim the old
committed documents had complete declarations. The separate default CLI command
includes today's worktree, including uncommitted pointer movement.

Validation results: default guard PASS; reproducible verifier PASS (23 controls);
maintained guard suite 77/77; changed-story CI suite 78/78; whitespace PASS.
The CI contract check uses the current Markdown and real committed history
without creating a new event commit. It is not a claim that an uncommitted
workspace has passed a hosted CI run. Application, Aspire and browser reruns do
not exercise this documentation repair.

The [raw-log archive](eventstore-gitlink-reconciliation-logs-2026-10-29.zip)
contains before/after command output, both maintained suite logs, raw pointer and
story history, the entry worktree observation, original/final spec/ledger/context
bytes and the preservation report. Its digest and each required command digest/result are checked by the
verifier, together with ZIP integrity. The two old-document controls use those
archived bytes, so the original temporary file is not required for reproduction.
Pre-existing spec sections and the effective frozen intent are preserved; the
effective intent is also anchored to entry Git because the creation intent was
legitimately renegotiated before this repair. New evidence whitespace is checked
explicitly, in addition to `git diff --check` for tracked edits.

Independent Blind Hunter review returned ten verification gaps. All were patched
as recorded in the spec's G1–G10 triage; no pointer-target defect or validator
change was found. Independent recheck confirms all ten findings addressed and
all earlier counterexamples rejected, with no remaining concrete issue.

Final preservation checks pass: HEAD, index bytes and gitlink stage entries are
unchanged; the staged diff is empty; production validators and dependency
configuration match entry. Archived before/after snapshots prove the ledger
changed only by one resolution line and project context only by the scoped
reconciliation sentence. Concurrent paging and UI edits were observed during the
run and left untouched. Their application behavior is outside this audit repair;
the retained package verification remains evidence of its original capture.
All required checks pass and the spec is done.
No staging, commit, push, dependency edit or gitlink movement is authorized or
performed in this workspace.
