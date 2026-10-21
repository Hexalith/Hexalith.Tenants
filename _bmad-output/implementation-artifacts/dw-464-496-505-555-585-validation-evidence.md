# Five deferred Story Guards CI records

Verified on 2026-10-21 over entry revision
`b02452a16869f265c21b1ae6c15f2f299677f139`. This documentation change closes
only **DW-464, DW-496, DW-505, DW-555 and DW-585**. Existing implementation
fixes are requalified; no browser, application, workflow or dependency code is
changed. Original observations and fields remain under their original or
`pre-ci-reconciliation-` keys.

## Existing fixes and hosted evidence

- `482b04769855531ec5630e01b4c0403444bad0d5` installs the matching Chrome
  headless shell for DOM capture while keeping full Chrome for the accessibility
  tree. `579ec157ac38eb30593e736042aedac4d1acd08d` captures through DevTools
  after the fixture reports a validation status, including asynchronous viewport
  updates. The original exit-134 and later profile-shipped timeout observations
  remain valid historical failures.
- The [first passing browser job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/39167197905/job/117792500177)
  runs at `579ec157ac38eb30593e736042aedac4d1acd08d`. The
  [later passing browser job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/39426910956/job/118641663695)
  runs at `3ae9db014dfe52bdee4f6731465cc2a7b65701b7` on 2026-10-20. Both
  install Chrome/headless shell **153.0.8010.52**, build the UI fixtures in
  **Release**, and complete the validation step successfully.
- Later-job log line 377 records rendered tenant start/preview/recovery in
  **EN/FR**, desktop, narrow and forced colors, recovery-reason DOM focus,
  rejected missing-tabindex control, static cancel/Escape focus helpers and zero
  fixture browser egress. Lines 378 and 384–386 record global correction
  preview/receipt checks, rejected return-true focus, hidden/in-flow dialog CSS
  and unstacked narrow recovery controls. These are completed scenario reports,
  rather than a successful install followed by skipped checks.
- `c2587b2654c7227d5fd6b6f3e33f341e9bc9ea14` adds actual changed-story
  enforcement in addition to regression tests. The
  [entry-head hosted gitlink job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/39659580488/job/119428776078)
  audits three committed stories, all passing, for
  `0c10ef637171f9ac2639d4b5ac42f513ea9ef133..b02452a16869f265c21b1ae6c15f2f299677f139`.
  It also passes the **49/77/13/31** test lanes. Its actual-artifact step, not
  just those regression lanes, settles DW-585.

The workflow, DOM capture helper and launcher have identical blobs at the later
successful browser revision and entry revision:

| File | Git blob at both revisions |
| --- | --- |
| `.github/workflows/story-guards.yml` | `6cfa604231c58dc9e507e5b4c24c88e2d6628855` |
| `tests/Hexalith.Tenants.UI.Tests/Browser/dump-validation-dom.mjs` | `5487b9b5d647755430185341244a20d06b96686c` |
| `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | `fd76f0d1e7c2b8963b53785c736231b811c2fc75` |

This equality qualifies the launch/enforcement implementation. It does not
equate all generated fixtures or application/dependency state across revisions.

## Separate current failure and scope limits

The [entry-head browser job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/39659580488/job/119428775389)
**fails before Chrome installation**. Its exact command is:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false
```

Exit **1**, zero warnings/one error:
`TenantReadAuthorityResult.cs(12,5): CS0246: NativeSourceProof could not be found`.
Both Chrome installation steps and browser validation are **skipped**. These
closures do not claim green current-head browser execution, all current pass-3
tests running, or release readiness. The independently failing package build
remains a gate; repairing it belongs to separate dependency/Contracts work.

The successful browser jobs exercise exported rendered fixtures with production
assets. They do not establish an authenticated interactive topology session,
human assistive-technology announcements, or every remaining modal focus and
responsive-layout question. Existing deferred records and release holds for
those requirements remain unchanged. No new workflow was dispatched and no
release was triggered during this evidence retrieval.

## Reproduce and verify

[Hosted evidence JSON](dw-464-496-505-555-585-hosted-evidence.json) retains the
three exact run responses, job/step conclusions, original numbered log excerpts,
raw response/log SHA-256 and byte counts, retrieval commands, and the Git blob
comparison. All seven retained `gh` commands exited 0. For example, from the
owning repository root:

```bash
gh run view 39426910956 --json headSha,jobs,url
gh api repos/Hexalith/Hexalith.Tenants/actions/jobs/118641663695/logs
gh run view 39659580488 --json headSha,jobs,url
gh api repos/Hexalith/Hexalith.Tenants/actions/jobs/119428776078/logs
gh api repos/Hexalith/Hexalith.Tenants/actions/jobs/119428775389/logs
python3 _bmad-output/implementation-artifacts/dw-464-496-505-555-585-verify.py
```

The [raw evidence archive](dw-464-496-505-555-585-raw-hosted-evidence.zip)
preserves all seven complete original responses/logs. The checker validates
their SHA-256, byte counts and exact excerpt positions, and binds each retrieval
to its expected repository/run/job. These checks establish consistency with the
retained primary-source responses; they are not independent cryptographic
authentication of GitHub. Re-fetch the cited endpoints for that comparison.

The retained checker is an offline fixed-snapshot evidence check. It verifies
all **657** IDs survive, exactly these five record bodies change, all old fields
survive, and the **465 done/192 open** disposition totals agree with the
maintenance table. **178** rows cover every open ID exactly once. It checks
unchanged unrelated maintenance rows, consistent closure status/disposition,
complete original field lines, metadata conclusions, exact event/artifact
reports and the three required historical blobs. It also recreates and replays
the event from `entry_event` in the retained JSON, without any pre-existing
temporary file.
Future unrelated ledger changes intentionally fail its fixed expectations;
replay it at its introducing commit when auditing this closure later.

Focused maintained checks:

- `python3 tests/scripts/test_validate_changed_story_gitlinks.py` — exit 0,
  **49/49**.
- `python3 tests/scripts/test_validate_story_gitlinks.py` — exit 0, **77/77**.
- `bash -n tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`
  and `node --check tests/Hexalith.Tenants.UI.Tests/Browser/dump-validation-dom.mjs`
  — exit 0.
- Initial direct replay used
  `python3 scripts/validate-changed-story-gitlinks.py --event-name push --event-path /tmp/dw-ci-reconciliation/entry-event.json`
  — exit 0, three actual committed stories pass. The retained JSON contains `before`
  `0c10ef637171f9ac2639d4b5ac42f513ea9ef133` and `after`
  `b02452a16869f265c21b1ae6c15f2f299677f139`, matching the hosted event.
  The checker command above now replays that input directly from the retained
  artifact; the original `/tmp` file is unnecessary.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-464-496-505-555-585-ci-evidence.md --ref b02452a16869f265c21b1ae6c15f2f299677f139`
  — exit 0, no pointer changes. The explicit committed tree excludes the
  pre-existing EventStore worktree pointer change from this evidence audit.

The first offline checker attempt failed because its Git reader stripped the
last record's trailing blank lines before comparison. Preserving the raw Git
text corrected that checker-only error; the final checker exits 0. The derived
maintenance summary also catches up its previously stale 455/202/186 totals;
the entry ledger actually held 460 done/197 open across 181 rows.
The first review-correction draft also rejected its expected evidence entry
set because `Counter` treated a dictionary's values as counts. Comparing its
keys corrected this checker-only draft failure before the final pass.

No application build or fresh local browser run is claimed for this
documentation scope. Current hosted failure evidence is recorded separately
above; narrow local checks do not replace that failed gate. The pre-existing
EventStore checkout pointer change is preserved and excluded from the commit.

## Independent review

Blind Hunter calculated `N = min(floor(sqrt(63.875) + 1), 10) = 8` and reported
eight evidence/reproducibility issues. All eight are patched: raw archive and
digest/excerpt checks; exact retrieval bindings and unique entries; mandatory
implementation paths/blobs; exact event/artifact pairs; preservation of unrelated
maintenance rows; consistent closure dispositions; complete field-line
preservation; and replay of the retained event. The workflow spec records each
verdict and evidence. No product defect or additional deferred item is introduced.
Independent recheck passes the corrected checker, including retained-event
replay, and rejects all eight focused harmful controls. It finds no remaining
concrete defects in the corrections. New local links and `git diff --check`
also pass.

## Commit validation

Repository-pinned `@commitlint/cli@21.2.3` matches the lockfile. The exact command
`node_modules/.bin/commitlint --edit /tmp/dw-464-496-505-555-585-commit-message.txt --verbose`
exits 0 with zero problems/warnings. Successful output is retained in
`/tmp/dw-464-496-505-555-585-commitlint.log`; the full candidate below has SHA-256
`e481a69978325ec534f231d827bea6d0c11bbdab3032e0d567bb040fa155f8d2`
(UTF-8, LF, final newline):

```text
docs: reconcile five deferred Story Guards CI records

Qualify existing Chrome 153 fixes and actual changed-story enforcement with
immutable hosted evidence and current guard regressions.

Close DW-464, DW-496, DW-505, DW-555, and DW-585 while preserving original
findings, the separate current build blocker, and unrelated deferred work.
```
