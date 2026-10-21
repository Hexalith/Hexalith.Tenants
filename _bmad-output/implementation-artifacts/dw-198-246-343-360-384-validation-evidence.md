# Five historical dependency-provenance records — 2026-10-21

Entry revision: `ffe9522b10f37f56cd9339c3387cbb4b7d16d897`, clean `main`.
Only DW-198, DW-246, DW-343, DW-360 and DW-384 are selected.

The repair makes historical ranges reproducible using existing tooling. It
changes four historical documents, the deferred ledger/index and one guidance
paragraph. It changes no application source, dependency pointer or validator.
Application, browser and Aspire runs do not apply to these documentation changes;
the original build/runtime observations and unresolved release holds remain
historical evidence, without a new acceptance claim.

## Historical ranges and attribution

| Artifact | Preserved baseline | Historical endpoint |
| --- | --- | --- |
| Story 2.4 | `29c4aec965e9cba4165a8844a86edc67ba7d756b` | `b2b80941df874c2ee6772ca316841c480e0e493b` |
| Story 2.4 WP-2A | `bb85fadb149fed1fa00dfd9c8d3315df541566e8` | `b2b80941df874c2ee6772ca316841c480e0e493b` |
| Story 4.2 grant | `7e88a571588fc7aa769ee1af01e91113f6f9b01f` | `cf0e420be4873e5d7a005316f3b056b192d3fb87` |
| Dependency refresh | `de5784ca751f51a4cfe282f67e11b13dd1ed4b45` | `d90b074e37bd31bc817b7c5f39d3245ba20738fc` |

The supplied **current Markdown** is checked against each historical Git tree.
`--ref` selects that tree; it does not load the old document. `gitlink_audit_ref`
lets the changed-story CI runner use the same endpoint in a pointer-free repair.
The direct validator still needs explicit `--ref`; its default HEAD comparison
continues to reject later unrelated dependencies. No guard behavior is weakened.

The final removal endpoint includes fixes and dependency moves after the earlier
`fa5ca559` initial implementation. Both removal artifacts declare exact targets
for seven range dependencies, with independent changes clearly distinguished
from story-carried pointers. The grant's seven existing targets already matched
its final endpoint; its repair adds a durable endpoint and complete attribution.
Original failure observations and owner decisions remain preserved.

The refresh's original baseline is a **mid-story checkpoint**. It is retained to
match the historical artifact and CI contract, with an explicit warning. Its
complete implementation window starts at
`b60f49c656723fb8cdff83a9164d1fa84b652707`, the parent of the first artifact
commit `cf31675b2e860c4be23e22355e361ce977da57c3`. Three mixed removal/refresh
commits carry the earlier Builds, EventStore, FrontComposer and Memories updates.
The topology alias landed in `a3321266b61fc98f98f700901bd2cc5166eff4b0`; both the
alias and missing dependencies now appear in the File List. The final scoped
`f5ed833a259f1599589ad8e7849ee4b8b849ba95` changes only Builds after the checkpoint.
Recorded-commit mode audits that move while retaining the mid-story warning;
the companion checker independently covers the earlier complete window.

[History manifest](dw-198-246-343-360-384-history.json) records canonical Git
objects, exact before/after pointers, complete changed-file sets and qualified
attribution. Pointer chains use first-parent landings, including merge commits,
so merged branch moves are not counted twice. Classifications describe which
work a commit carries; they do not prove a dependency update was behaviorally
required by a story.

## Reproducible checks

From the repository root:

```bash
python3 _bmad-output/implementation-artifacts/dw-198-246-343-360-384-verify.py
python3 tests/scripts/test_validate_story_gitlinks.py
python3 tests/scripts/test_validate_changed_story_gitlinks.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-198-246-343-360-384-historical-provenance.md
git diff --check
```

The companion checker verifies each historical CLI result, exact parsed target
to Git-tree equality, complete first-parent chains, changed-file attribution
inputs, frozen-block preservation and the refresh alias's actual introducing
diff. It rejects premature audit endpoints and runs wrong-target and missing-path
declaration controls in temporary Markdown copies. It also replays the entry
documents: removal fails on six undeclared paths, grant already passes, and
refresh fails the legacy mid-story-baseline check. Repaired refresh passes with
the warning retained. These are separate from the final changed-story event gate,
which runs against the real committed snapshots before push.

The ledger audit requires exactly five changed record bodies, all 657 unique
numeric IDs, lossless preservation of selected historical fields, and every open
ID indexed exactly once. DW-549 retains its remaining grouped row and policy
question after removing DW-198. Unrelated record bodies/rows, archive, sprint,
root declarations and all dependency pointers must match the entry revision.
The final inventory is 455 done / 202 open across 186 maintenance rows.

Initial regression results: validator 77/77 and changed-story CI 49/49 pass,
without failures/errors. Initial historical guards pass for all four artifacts;
the refresh emits its documented checkpoint warning. The first companion-checker
attempt found that ordinary Git log traversal did not model merge landings as
one sequential chain; the manifest and checker now explicitly use first-parent
history. No production code or guard policy changed to address that finding.

## Independent review and final verification

Review and final committed-event results are recorded below after completion.

Companion checker: **PASS**, with four historical passes, four moving-HEAD
refusals, four premature-endpoint refusals, 22 wrong-target refusals, 22
missing-declaration refusals and four entry-document replays. Exact history
counts are removal 13 commits / 34 changes per artifact, grant 28 / 76 and
refresh 4 / 12. The refresh's smaller checkpoint-to-endpoint range contains only
the one recorded Builds move; its 4 / 12 count covers the separate complete
implementation window. The final counts and control results are saved in
[validation JSON](dw-198-246-343-360-384-validation.json).

The checker initially expected a distinct exit code for the refresh's baseline
refusal; the existing CLI returns exit 1 for that refusal. Corrected the expected
code while retaining the actual refusal check. Both maintained regression suites,
the new-work spec gitlink audit and whitespace checks pass.

Commit-message validation used the repository lockfile's commitlint **21.2.3**:
`node_modules/.bin/commitlint --edit /tmp/dw-historical-provenance/commit-message.txt --verbose`
exited 0 with **0 problems, 0 warnings** for the exact full candidate below
(including its final newline):

```text
docs: repair five historical dependency provenance records
```

`git fetch origin main` succeeded and `git rev-list --left-right --count
HEAD...origin/main` returned `0 0` before commit. The user explicitly requested
commit/push on main. Committed-snapshot event validation runs before push.

The independent Blind Hunter review inspected 454.566 kB; its finding floor was
`min(floor(sqrt(454.566) + 1), 10) = 10`. All ten findings are patched in scope:

1. DW-549's surviving index row now reflects only its awaiting-decision status,
   Story 5.6 dependency and concrete next action. Its ledger record is preserved.
2. Binding table baseline and target cells must both equal Git's exact pointers.
3. The refresh's separate complete-window table is checked against its actual
   implementation start and completion tree, including all four rows.
4. Parsed binding targets must exactly cover the expected changed paths with full
   canonical IDs; dropping targets cannot silently reduce the mutation matrix.
5. Default checker execution compares the recomputed result with saved JSON.
   `--write-result` explicitly regenerates that file after a successful audit.
6. Merge parent and merged-commit lists are checked against Git.
7. Attribution categories must be defined and carry supporting artifact/code
   paths present in the verified diff. The grant-preview commit's reason now
   accurately names its new preview type instead of claiming a grant-spec edit.
   Categories remain source-inspected judgments; pointer presence alone does not
   imply a behavioral requirement.
8. Original failure replays require the six actual undeclared paths or specific
   mid-story diagnostic, and the refresh must emit its exact checkpoint warning.
9. Three additional controls reject the superseded initial removal and grant-core
   endpoints, specifically because they omit later story-associated pointer work.
10. Fixed-count evidence is explicitly scoped to its immutable repair snapshot;
    the replay procedure below isolates it from later ledger changes.

The initial attribution-support check exposed that grant implementation commit
`8da765adbc112c75fd1a120f42fdcd0fb493734e` changes grant code but not the grant
spec. The manifest now cites `GlobalAdministratorGrantPreview.cs`, its actual
supporting implementation path. No historical Git object or classification is
changed to invent a spec edit.

## Replay after main advances

This checker deliberately audits this repair's fixed ledger snapshot. Running it
against a later main with other closures should fail its preservation/count
checks. Resolve the full introducing revision of the uniquely added checker and
use a temporary detached worktree; no submodule initialization is needed:

```bash
audit_revision=$(git log --diff-filter=A --format=%H -- _bmad-output/implementation-artifacts/dw-198-246-343-360-384-verify.py)
test "${#audit_revision}" -eq 40
audit_checkout=$(mktemp -d /tmp/tenants-dw-history.XXXXXX)
git worktree add --detach "$audit_checkout" "$audit_revision"
(cd "$audit_checkout" && python3 _bmad-output/implementation-artifacts/dw-198-246-343-360-384-verify.py)
git worktree remove "$audit_checkout"
```

The introducing revision is the immutable evidence snapshot, rather than the
moving last edit or repository HEAD. Do not regenerate saved results to make an
unrelated later snapshot pass. The normal command compares the stored evidence;
`--write-result` is for an intentionally revised audit during this repair.

Final independent recheck resolves all ten findings with none remaining or newly
deferred. The normal checker passes **63 controls** and exact saved-result
equality; seven focused review probes reject bad table endpoints, absent targets,
invalid merge/category evidence and stale saved JSON. Original ledger contents,
all 657 IDs, exact 202-open-ID maintenance coverage and dependency pointers are
preserved. Both unchanged regression suites pass 77/77 and 49/49. Final whitespace
and new-work gitlink checks pass. The committed-snapshot event gate and detached
replay are the final checks before the explicitly authorized push.
