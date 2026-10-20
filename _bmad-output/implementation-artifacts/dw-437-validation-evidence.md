# DW-437/DW-451/DW-534 validation evidence — 2026-10-20

Entry revision: `fc2d4821e002ea56eaed79b2acae4cc0bfc8ef10`, clean main. These
three records comprise the first actionable maintenance row. DW-451 and DW-534
are duplicate recurrences of DW-437; historical attribution work in DW-198 stays
open. No application, dependency, submodule checkout, sprint or release change is
part of this task. User authorization includes committing and pushing the change.

## Behavior and limits

The Story Guards gitlink job now checks changed, committed story Markdown using
the existing production validator. Full history is mandatory. Pushes use the
event before/after commits; PRs use merge-base(base, head) and the PR head, rather
than GitHub's merge checkout. Markdown comes from that head tree, so worktree
edits cannot change the evidence. Every selected story runs and failures aggregate.

Historical repairs may explicitly record `gitlink_audit_ref`. The runner requires
a canonical available endpoint at or before event base, the story and its same
baseline at that endpoint, and an event with **no gitlink-bearing commits**. A
move later reverted or introduced through a merge still refuses historical mode.
This deliberately requires isolating historical repairs from dependency work.
Current Markdown is audited against the chosen historical Git tree, as with the
existing `--ref` interface; the runner does not claim to load historical Markdown
for the actual declaration check. See [operator details](../../docs/story-gitlink-ci.md).

Unchanged historical stories are excluded. Pointer-only pushes without changed
stories do not acquire guessed ownership. Zero/missing/unavailable event commits,
nonancestor push ranges and shallow history refuse rather than invent a range.
Ordinary evidence Markdown is not a story. Conventional specs/numeric stories and
old/new baseline metadata prevent metadata removal or a rename from silently
removing an existing story from selection. Story identities touched in any event commit remain selected even after a
restore; deleted identities always refuse, and symlink/type replacements refuse. Existing guard semantics are unchanged.

## Executed checks

From the repository root:

```bash
python3 tests/scripts/test_validate_changed_story_gitlinks.py
python3 tests/scripts/test_validate_story_gitlinks.py
python3 tests/scripts/test_pushall_detached_head.py
python3 tests/scripts/test_pushall_default_branch.py
actionlint .github/workflows/story-guards.yml
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-437-changed-story-ci-gate.md
git diff --check
```

The initial new lane passed **33/33**. Retained lanes pass **77/77**, **13/13**, **31/31**,
all with zero failures/skips. Actionlint, own-story validation and whitespace
checks exit 0. The initial new-lane run had three failures because its positive
fixtures put a path and arrow on one list item, which does not satisfy the
existing standalone declaration contract. Fixing fixtures to use a standalone
path plus the supported target table produced the passing result; production
guard behavior was not changed.

The new tests run the real runner and production guard, without a network remote
or submodule initialization. They cover declared/undeclared/misstated movement,
later pointer commits in one push, all-story failure aggregation, numeric/space
paths, metadata removal, renames, committed-snapshot isolation, PR merge-base/head
selection despite independent target advancement, strict historical boundaries,
pointer/reversion/merge refusal, deletion/type changes, owned-chain acceptance and
unrecorded endpoint refusal, malformed evidence and shallow checkouts.

## Reproducible negative controls

This recipe changes only disposable copies of the runner and runs the maintained
fixtures against each variant. It must reject every variant through assertion
failures, with no test errors. Temporary Git commit messages use the separately
validated fixture candidate below.

```bash
python3 - <<'PY'
from pathlib import Path
import importlib.util, io, shutil, tempfile, unittest
root = Path.cwd()
source = (root / 'scripts/validate-changed-story-gitlinks.py').read_text()
variants = {
    'ignore-production-verdict': ('refused |= result.returncode != 0', 'refused |= False'),
    'ignore-historical-pointer-commits': ('if moved:\n        raise guard.CheckError(', 'if False:\n        raise guard.CheckError('),
    'compare-pr-target-instead-of-head': ('head = canonical_commit(event["pull_request"]["head"]["sha"])', 'head = target'),
}
for name, (old, new) in variants.items():
    if source.count(old) != 1:
        raise RuntimeError((name, 'variant no longer matches exactly once'))
    with tempfile.TemporaryDirectory(prefix='dw-437-control-') as directory:
        candidate = Path(directory)
        (candidate / 'scripts').mkdir()
        (candidate / 'scripts/validate-changed-story-gitlinks.py').write_text(source.replace(old, new))
        shutil.copyfile(root / 'scripts/validate-story-gitlinks.py', candidate / 'scripts/validate-story-gitlinks.py')
        spec = importlib.util.spec_from_file_location('changed_story_tests', root / 'tests/scripts/test_validate_changed_story_gitlinks.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        module.ROOT = candidate
        result = unittest.TextTestRunner(stream=io.StringIO()).run(unittest.defaultTestLoader.loadTestsFromModule(module))
        print(name, result.testsRun, len(result.failures), len(result.errors))
        if not result.failures or result.errors:
            raise RuntimeError('Variant was not cleanly rejected')
PY
```

The initial three controls produced **10**, **3**, **1** assertion failures and
zero errors across 33 tests each. The recipe above targets the final runner; its
updated qualification is recorded below. The production source remains untouched.

## Commit validation and preservation

Pinned `@commitlint/cli@21.2.3` validates the exact full task candidate
`ci(tooling): enforce gitlink checks for changed stories` and fixture candidate
`test: Create fixture commit`. Both commands exit 0, `found 0 problems, 0 warnings`:

```bash
python3 - <<'PY'
from pathlib import Path
directory = Path('/tmp/dw-437')
directory.mkdir(exist_ok=True)
(directory / 'commit-message.txt').write_text('ci(tooling): enforce gitlink checks for changed stories\n')
(directory / 'fixture-message.txt').write_text('test: Create fixture commit\n')
PY
node_modules/.bin/commitlint --edit /tmp/dw-437/commit-message.txt --verbose
node_modules/.bin/commitlint --edit /tmp/dw-437/fixture-message.txt --verbose
```

Only DW-437/DW-451/DW-534 are resolved. Their changed fields retain original values
as `pre-dw-437-*`; original findings, priorities, ownership and dependencies remain.
The single grouped maintenance row is removed. Expected inventory: all **656** IDs,
**394 done / 262 open**, covered exactly once by **236** maintenance rows. No
archive move or unrelated status change is intended. Independent review, preservation and committed-event replay results follow.

The read-only preservation comparison against the entry revision passes: 656
unchanged IDs, exactly the three selected closures, 394 done/262 open, and 236
maintenance rows covering every open ID once. Every unrelated ledger record and
maintenance row is byte-preserved. The archive, sprint status, release readiness
and dependency pointers match entry. Syntax parsing of both new Python files
also passes.

A disposable shared-object clone of the root repository was checked out at the
entry commit without initializing submodules. Copying the eight selected files,
committing with the validated task candidate, and replaying a push event from
entry to that temporary commit selects exactly this new spec and exits 0 with
`Changed stories: 1; failed: 0`. Neither the workspace's index/refs nor its
dependency checkouts are changed by this replay. The temporary commit is test
evidence, not the final published revision.

Origin was fetched without submodule recursion and remained aligned with the
entry revision (zero commits on either side). The tracked project context's
feature-branch rule requires the task commit to use
`fix/dw-437-changed-story-gitlink-gate`; the completed branch will be pushed there.
No application build, Aspire run or browser execution is relevant to this Python
and CI change. Existing independent release/browser qualification holds remain.

## Final independent qualification

Blind Hunter reviewed approximately 49.918 kB of the initial change and returned
nine findings (floor eight). The spec retains every disposition. All nine were
patched: baseline narrowing, final-diff-only selection, original rename identity,
CR path transport, premature historical endpoints, retargeted PR activity,
regression continuation after refusal, evidence symlink exclusion and ambiguous
rewritten renames. No finding is deferred.

Correction recheck additionally reproduced historical rename-away/rename-back
concealment. The historical scan now follows rename lineage, includes prior
basenames and rejects deleted/recreated identities. A separate CR-baseline
control exposed Git's quoted filename heuristic; exact literal-path inspection
now qualifies that legacy check too. Final positive controls retain valid
historical-after-rename audits and baseline broadening.

Intermediate qualification recorded 45/45, then 46 tests with one expected-error
diagnostic mismatch after stricter refusal. The assertion was corrected. Initial
historical-follow qualification had five failures because diff-merges enabled
patch output in the commit-ID query; explicit no-patch output fixed the transport
and the creation-boundary diagnostic was updated. These were corrected before
final qualification, with no production refusal weakened.

The final runner suite passes **49/49**, zero failures/skips. Together with the
retained **77/13/31** lanes this is **170 passing tests**. Final isolated controls
using the recipe above reject ignored production verdicts, disabled event-pointer
restriction and wrong PR targets with **11/3/2** assertion failures, respectively,
across 49 tests each, zero errors. Actionlint, whitespace, own-story guard and
pinned commitlint pass.

The independent final correction recheck reports no remaining concrete findings:
49/49 maintained tests, all 13 targeted correction controls, and extra merged
history rename/revert controls pass. The reviewer also confirms workflow
conditions and final documentation agree. PR edited activity was verified using
[GitHub's event reference](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request).

Final rules supersede the initial deletion allowance: story identities must
survive; all event commits participate in selection; baselines precede event base
and may only broaden earlier baselines; historical endpoints must not omit later
pointer-bearing work under any followed story identity. Unrelated old story and
release-attribution questions remain open under their original IDs.
