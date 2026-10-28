# Story gitlink CI check

Story Guards runs `scripts/validate-changed-story-gitlinks.py` on pushes and pull
requests to main, in addition to the validator's regression suites. Checkout must
use full history (`fetch-depth: 0`); no submodule initialization is needed.
PR activity includes `edited` so retargeting to main reruns the gate. All four
regression lanes continue after an artifact refusal unless the job is cancelled.

The runner reads `GITHUB_EVENT_PATH` and `GITHUB_EVENT_NAME`. Pushes select the
`before..after` range. Pull requests select `merge-base(base.sha, head.sha)..head.sha`,
so unrelated base-branch changes and GitHub's synthetic merge commit are excluded.
Unavailable commits, shallow history, unsupported events and nonancestor push
ranges fail with a diagnostic. Initial pushes with a zero before ID require an
established comparison history; they do not silently pass.

Markdown touched in **any commit** of the event beneath
`_bmad-output/implementation-artifacts/` is a story when
its filename starts with `spec-` or a numeric epic/story pair, or either snapshot
has `baseline_commit` frontmatter. Evidence documents without these markers are
excluded before regular-file enforcement; symlink targets are never followed.
Renames retain their old/new identity, including original basenames for baseline
checks. Restoring a story's original contents before the head does not remove it
from selection. Removed stories fail closed: retain their Markdown identity so
an undetected rewrite/rename cannot silently become untracked evidence.

The runner materializes each story from the event's head tree and invokes
`validate-story-gitlinks.py <snapshot> --ref <full-head-commit>`. It preserves the
basename for the existing mid-story-baseline check. All selected stories are
checked, and any refusal fails the job. Local worktree changes cannot alter this
committed-snapshot audit. The production guard still enforces its existing
baseline, declaration, target and Story-Owned Gitlink Commits contracts.
The declared baseline must be at or before the event base; a replacement may
broaden an existing baseline but cannot narrow it. Separate a story whose true
start is inside a larger push into its own event range rather than hide earlier
event pointers behind a later baseline.

For a documentation correction to a historical story, explicitly add
`gitlink_audit_ref: '<full-canonical-historical-commit>'` to its frontmatter. The
endpoint must be available, at or before the event base, and contain the story
with the same `baseline_commit`; that baseline must precede the endpoint. The
current Markdown is checked against that historical Git tree. An endpoint also
refuses if later commits before the event base touch the same story while carrying
gitlinks. An initial snapshot therefore cannot hide later story-associated pointer
work. Later prose-only edits may follow the selected endpoint; it need not be the
last story edit. No endpoint is guessed from prose or the last file edit.

One narrowly verified correction is available when the original baseline is an
unavailable full object ID. Preserve that exact value as
`gitlink_original_baseline`, record the original creation commit as
`gitlink_creation_ref`, set `baseline_commit` to that creation's exact single
parent, and supply `gitlink_audit_ref` for the historical pointer-work endpoint. All
revision fields must be full canonical IDs; the corrected baseline, creation
and endpoint must be available commits in the required ancestry.

To audit the corrected Story 5.4 Markdown directly at its recorded endpoint:

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md --ref 4f426e591c85825eb41ec4b289a43e91f2085004
```

The runner independently inspects full history through the event head for the
story's literal identities, discovers renames across the repository, and requires
a sole original creation. An introduction already present in another merge
parent retains that parent's original creation; a story first created in a merge
still fails the exact single-parent requirement.
Its original committed Markdown and the endpoint Markdown must both record the
preserved unavailable baseline. Deleted/recreated identities, overlapping or
unresolved rename identities, multiple parents at creation, false provenance,
and an available original object all fail closed. The event-base snapshot must
still satisfy the existing baseline comparison, allowing the preserved original
only after this independent verification. A later documentation edit can retain
the same verified correction metadata. This is not a general mechanism to
replace valid baselines or move them forward.

A recognized historical rename must be the only repository add/delete/rename in
its commit. Competing identity changes make the correction ambiguous even when
Git picks a plausible source (for example, two identical deleted files). Other
artifact modifications may share that commit. This conservative rule applies
only to the optional malformed-baseline correction.

An original-creation claim also fails if its commit removes or renames another
Markdown source anywhere in the repository. A heavily rewritten import can
appear as an addition plus deletion instead of a Git rename; the runner treats
that possible origin as ambiguous rather than inventing a new starting point.
This conservative check applies to original creation, including a creation
absent from all merge parents, rather than branch work merely introduced by a
merge. Unrelated Markdown modifications do not trigger it.

Corrected historical audits require a stated target for every pointer in the
baseline-to-endpoint range. The production guard remains responsible for path
declarations and matching those targets to the selected historical tree. Keep
unrelated pointers in that inventory as historical range contents, without
claiming runtime ownership. All existing identity, ancestry, later
story-associated pointer, and pointer-bearing event refusals still apply.
Story 5.4's verified correction and original tooling attribution are recorded in
[DW-133/365/461/467/478 evidence](../_bmad-output/implementation-artifacts/dw-133-365-461-467-478-validation-evidence.md).

A historical audit is refused if **any** commit in the event range changes a
`references/` gitlink, including changes later reverted or introduced by merges.
Keep a historical documentation repair separate from dependency changes. This
restriction prevents a pinned old endpoint from hiding pointers carried by new
story work. Unchanged old stories are not selected, and independent pointer-only
pushes do not acquire inferred story ownership. Historical range inventories for the removal, grant and dependency-refresh
artifacts are recorded in [DW-198/246/343/360/384 evidence](../_bmad-output/implementation-artifacts/dw-198-246-343-360-384-validation-evidence.md).
Other historical range-policy decisions remain separately tracked in DW-549.

To replay a local event, save its JSON and run:

```bash
python3 scripts/validate-changed-story-gitlinks.py --event-name push --event-path /tmp/story-event.json
```

A push payload needs `before` and `after` full commit IDs. A pull-request payload
needs `pull_request.base.sha` and `pull_request.head.sha`; use `--event-name
pull_request`. No remote operations occur. Run the maintained integration lane
with `python3 tests/scripts/test_validate_changed_story_gitlinks.py`.
