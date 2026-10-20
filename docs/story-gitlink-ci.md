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
gitlinks. An initial snapshot therefore cannot stand in for its later closure.
No endpoint is guessed from prose or the last file edit.

A historical audit is refused if **any** commit in the event range changes a
`references/` gitlink, including changes later reverted or introduced by merges.
Keep a historical documentation repair separate from dependency changes. This
restriction prevents a pinned old endpoint from hiding pointers carried by new
story work. Unchanged old stories are not selected, and independent pointer-only
pushes do not acquire inferred story ownership. Historical attribution questions
outside the selected range remain separately tracked in DW-198.

To replay a local event, save its JSON and run:

```bash
python3 scripts/validate-changed-story-gitlinks.py --event-name push --event-path /tmp/story-event.json
```

A push payload needs `before` and `after` full commit IDs. A pull-request payload
needs `pull_request.base.sha` and `pull_request.head.sha`; use `--event-name
pull_request`. No remote operations occur. Run the maintained integration lane
with `python3 tests/scripts/test_validate_changed_story_gitlinks.py`.
