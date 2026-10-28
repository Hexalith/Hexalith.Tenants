# DW-133/DW-365/DW-461/DW-467/DW-478 validation evidence

Entry revision: `7ddb864cda5e942925b056391617eae4895c1253` (2026-10-28).
Scope is the approved [historical guard spec](spec-dw-133-365-461-467-478-historical-guard.md).
This is tooling and historical documentation qualification. It does not rerun
or newly qualify the historical application, .NET, browser, deployment or release
results retained in the source stories.

## Historical provenance and attribution

The original Story 5.4 baseline
`a5ca6e3f548e89b28a37826be721d9ef9f7cd51a` is an unavailable object. Its sole
creation is `8a24128989e348768173875ffb3c38bbd7461657`, whose exact single parent
is `a5ca6e3886bf0623a1ff8da8cdfd6fade8516652`. The final historical endpoint is
`4f426e591c85825eb41ec4b289a43e91f2085004`; the artifact has no later edits
through entry. Creation and endpoint both preserve the malformed original in
their original committed Markdown. Current Markdown retains it as
`gitlink_original_baseline`, records `gitlink_creation_ref` and
`gitlink_audit_ref`, and inventories all seven absent-at-baseline additions with
their full endpoint targets. Unrelated historical pointers remain range contents,
without being relabeled as Story 5.4 runtime work.

DW-133 is resolved historical attribution: original commit
`2e61f57bda6379192007d1bc6fabbde61996b11d` adds the gitlink instructions to the
three recorded rendered workflow paths (11/11/2 inserted lines). Story 1.10's
unchanged File List already calls these regenerated tooling rather than runtime
implementation. The executable assertions below inspect the original diff and
confirm that adding the validator invocation is exactly reproducible.

DW-365 is resolved historical attribution: original commit
`5d6601edf357772c80f5c02c487742eace0739e2` adds all three pushall manifests,
each with blob `060aed7d69b23c626936374be23a6cd009b7c678` (120 lines each).
The unchanged removal story already excludes this later tooling from removal
ownership. Its retained original `git add -A` and build-subject behavior is
historical evidence, not an instruction to execute those skill copies.

The earlier [accepted-dispositions draft](spec-dw-117-118-133-365-367-accepted-dispositions.md)
withdrew blanket closure of accepted limitations because the index keeps them
visible. That draft remains unchanged. This approved scope closes DW-133/DW-365
through independently verified attribution, preserving their former accepted
dispositions and limitations under `pre-historical-guard-` fields. It does not
rewrite or split mixed historical commits or change the general visibility
policy. Reopen attribution if new evidence contradicts the recorded original
diffs/blobs; scope future tooling changes separately. Pushall procedure findings
retain their separate DW-417/DW-418/DW-428/DW-429 history. DW-117/DW-118/DW-367
remain open under their existing accepted limitations.

## Executed checks

2026-10-28 initial local implementation validation:

- `python3 tests/scripts/test_validate_story_gitlinks.py`: **77 tests, OK**.
- `python3 tests/scripts/test_validate_changed_story_gitlinks.py`: **69 tests,
  OK**; 49 existing cases plus 20 new cases with negative subcases, all invoking
  the actual runner and production guard on committed disposable histories.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md --ref 4f426e591c85825eb41ec4b289a43e91f2085004`:
  exit 0, **7 declared additions, RESULT: PASS**.
- The actual runner's `audit_ref` was also invoked on the corrected real artifact
  with entry as both event boundaries and `moved=False`; it returned precisely
  `4f426e591c85825eb41ec4b289a43e91f2085004` (exit 0). This local metadata probe
  complements the committed fixture tests; the final committed-event replay is
  recorded by the parent below.
- `git diff --check`: exit 0, no output.

The first 68-case run exposed three fixture failures: two deletion fixtures tried
to remove their intentionally modified local story without `git rm -f`, and one
short-original subcase expected a later canonical-ID diagnostic when the earlier
unavailable prior-baseline check already refused it. Corrected the disposable
fixtures/diagnostic expectation and reran both suites successfully. No refusal
was relaxed to make those fixtures pass. The first inline attribution assertion
expected the shorthand `git add -A`; the original manifest spells it
`git -C <dir> add -A`. Corrected the assertion to that exact original command
and reran the full block.

The accepted-correction fixture retains unrelated later dependency movement
while checking the recorded historical target. Negative fixtures cover absent
metadata, missing/false provenance, unavailable/false creation, available
original commits or blobs, wrong exact parent, abbreviated revisions,
recreation, ambiguous rewritten rename, omitted/wrong targets and declarations,
later story-associated pointer work, and pointer-bearing events including a
move/revert sequence. Existing merge, ancestry, identity, endpoint and
production target regressions continue to pass. A final committed-history case
also refuses Git's plausible rename selection between two identical deleted
sources: a correction rename must be the only repository add/delete/rename in its
commit. This conservative ambiguity rule does not affect stories without repair
metadata.

Review-fix focused validation on 2026-10-28 runs only
`python3 tests/scripts/test_validate_changed_story_gitlinks.py`. The new cases
cover deletion/recreation inside the repair event, a genuine branch creation
introduced by merge, refusal of a first creation absent from all parents of a
multi-parent merge, an invented creation baseline at a `docs/` import,
coexisting historical identities, and an absent-at-baseline addition paired
with a removal, including missing declarations and omitted/wrong targets.
The first 77-case run found one fixture failure: identical empty branch commits
collapsed to the same commit, leaving no merge to test. A distinct branch file
now guarantees the intended two-parent history; no production refusal changed
for that fixture repair. The final focused run passed **77 tests, OK** (exit 0,
13.847 seconds). Broader verification remains parent-owned.

The next review pass isolated parent multiplicity by recording main's advanced
commit as the multi-parent fixture's exact first-parent baseline. A temporary
copy replacing the complete parent-list check with a first-parent-only check
was rejected by that one test: the mutated runner incorrectly passed its
committed event, causing `AssertionError: 0 == 0` (one test, one expected failure).
The real working-tree runner was never mutated. The executable probe below
exited 0 after checking that exact failure.

The rewritten-import regression asserts Git actually reports an added artifact
and deleted `docs/old-story.md`, with rewritten content below the rename
similarity threshold. Original creation now refuses any competing removed or
renamed Markdown source repository-wide; branch introductions already present
in another merge parent retain their original creation and are unaffected.
The original Story 5.4 creation's Markdown diff contains one addition and two
modifications, with no removed or renamed source. The first 78-case run reached
the new refusal earlier in an existing rewritten-rename test; its stale
diagnostic expectation was updated without weakening the refusal. Final focused
command `python3 tests/scripts/test_validate_changed_story_gitlinks.py` passed
**78 tests, OK** (exit 0, 13.288 seconds). No broader test lane was run in this
review-fix pass.

Run this complete assertion block from the repository root to reproduce the Git,
attribution and preservation evidence. It uses only local history and does not
load or execute historical skills:

```bash
python3 - <<'PY'
from collections import Counter
from pathlib import Path
import re
import subprocess

ENTRY = '7ddb864cda5e942925b056391617eae4895c1253'
ORIGINAL = 'a5ca6e3f548e89b28a37826be721d9ef9f7cd51a'
BASELINE = 'a5ca6e3886bf0623a1ff8da8cdfd6fade8516652'
CREATION = '8a24128989e348768173875ffb3c38bbd7461657'
ENDPOINT = '4f426e591c85825eb41ec4b289a43e91f2085004'
ARTIFACTS = '_bmad-output/implementation-artifacts/'
STORY = ARTIFACTS + '5-4-audit-availability-state-recovery.md'
SELECTED = {133, 365, 461, 467, 478}

def git(*args):
    return subprocess.check_output(['git', '--literal-pathspecs', *args])

missing = subprocess.run(['git', 'cat-file', '-e', ORIGINAL], capture_output=True)
assert missing.returncode == 1, missing.stderr
assert git('log', '--follow', '--diff-filter=A', '--format=%H', '--', STORY).split() == [CREATION.encode()]
assert git('show', '-s', '--format=%P', CREATION).strip() == BASELINE.encode()
assert git('log', '--format=%H', ENDPOINT + '..' + ENTRY, '--', STORY) == b''
for older, newer in [(BASELINE, CREATION), (CREATION, ENDPOINT), (ENDPOINT, ENTRY)]:
    git('merge-base', '--is-ancestor', older, newer)
for revision in [CREATION, ENDPOINT]:
    assert b'baseline_commit: ' + ORIGINAL.encode() in git('show', revision + ':' + STORY)

targets = {
    'Hexalith.AI.Tools': '3f194e17174994d308ec84af9ee2b5aa68674d0d',
    'Hexalith.Builds': '611f7e882ec615bad534de73149c7e95431b4cd4',
    'Hexalith.Commons': 'c13dc6679aa91144b6d541078f3f20019d79c2eb',
    'Hexalith.EventStore': '4339eb6aa4d52b83adc558d2c03687b7ac7d43f2',
    'Hexalith.FrontComposer': '3b1584d923a9a7dac163050b924285a1206605b2',
    'Hexalith.Memories': 'ece4edc4c9a37a62b34d3b7c8aa901fc363c038c',
    'Hexalith.PolymorphicSerializations': '7a2134eefa689d026a3fac814dd8732b22a43723',
}
raw = git('diff', '--raw', '--no-abbrev', BASELINE, ENDPOINT, '--', 'references/').decode()
actual = {}
for line in raw.splitlines():
    metadata, path = line.split('\t')
    old_mode, new_mode, old_oid, new_oid, status = metadata.split()
    assert (old_mode, new_mode, old_oid, status) == (':000000', '160000', '0' * 40, 'A')
    actual[path.removeprefix('references/')] = new_oid
assert actual == targets
current_story = Path(STORY).read_text()
for name, oid in targets.items():
    assert f'- `references/{name}`\n' in current_story
    assert f'| `references/{name}` | absent | `{oid}` |' in current_story
print('PASS: unavailable original, sole creation, exact parent/endpoint, seven exact additions')

render_commit = '2e61f57bda6379192007d1bc6fabbde61996b11d'
render_paths = ['_bmad/render/bmad-quick-dev/' + name for name in
                ['step-05-present.md', 'step-oneshot.md', 'workflow.md']]
changes = git('diff-tree', '--no-commit-id', '--name-status', '-r', render_commit, '--', *render_paths).decode()
assert changes.splitlines() == ['M\t' + path for path in render_paths]
for path, count in zip(render_paths, [11, 11, 2]):
    assert git('diff', '--numstat', render_commit + '^', render_commit, '--', path).decode().split()[:2] == [str(count), '0']
    patch = git('diff', render_commit + '^', render_commit, '--', path).decode()
    assert any(line.startswith('+') and not line.startswith('+++') and
               'python3 scripts/validate-story-gitlinks.py <spec-file>' in line
               for line in patch.splitlines())
pushall_commit = '5d6601edf357772c80f5c02c487742eace0739e2'
pushall_paths = [prefix + '/skills/pushall/SKILL.md' for prefix in ['.agents', '.claude', '.github']]
changes = git('diff-tree', '--no-commit-id', '--name-status', '-r', pushall_commit, '--', *pushall_paths).decode()
assert changes.splitlines() == ['A\t' + path for path in pushall_paths]
for path in pushall_paths:
    assert git('rev-parse', pushall_commit + ':' + path).strip() == b'060aed7d69b23c626936374be23a6cd009b7c678'
    blob = git('show', pushall_commit + ':' + path)
    assert len(blob.splitlines()) == 120 and b'git -C <dir> add -A' in blob and b'build:' in blob
source_stories = [
    'spec-1-10-direct-tenants-reads-and-authoritative-freshness.md',
    'spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md',
    'spec-dw-117-118-133-365-367-accepted-dispositions.md',
]
for name in source_stories:
    assert Path(ARTIFACTS + name).read_bytes() == git('show', ENTRY + ':' + ARTIFACTS + name)
assert not git('diff', ENTRY, '--', '_bmad/', '.agents/', '.claude/', '.github/skills/')
print('PASS: original tooling diffs/blobs, source stories and superseded draft unchanged, generated tooling unchanged')

ledger_path = ARTIFACTS + 'deferred-work.md'
def records(text):
    starts = list(re.finditer(r'^### DW-(\d+):', text, re.M))
    result = {}
    for index, match in enumerate(starts):
        number = int(match[1])
        assert number not in result
        end = starts[index + 1].start() if index + 1 < len(starts) else len(text)
        result[number] = text[match.start():end]
    return result
old = records(git('show', ENTRY + ':' + ledger_path).decode())
new = records(Path(ledger_path).read_text())
assert old.keys() == new.keys() and len(new) == 657
assert {number for number in new if old[number] != new[number]} == SELECTED
assert sum(old[number] == new[number] for number in new) == 652
for number in SELECTED:
    restored = re.sub(
        r'^pre-historical-guard-(status|disposition|verification-date|verification|next-action): ([^\n]*)\n\1: [^\n]*$',
        lambda m: m[1] + ': ' + m[2], new[number], flags=re.M,
    )
    assert restored == old[number], number
    assert re.search(r'^status: done 2026-10-28$', new[number], re.M)
    assert re.search(r'^disposition: done$', new[number], re.M)
def done(record):
    return bool(re.search(r'^status: done(?: |$)', record, re.M))
assert sum(map(done, old.values())) == 629
assert sum(map(done, new.values())) == 634
open_ids = {number for number, record in new.items() if not done(record)}
assert len(open_ids) == 23
backlog = Path('docs/maintenance-backlog.md').read_text()
rows = [line for line in backlog.splitlines() if line.startswith('| DW-')]
coverage = Counter(int(number) for row in rows for number in re.findall(r'DW-(\d+)', row.split('|')[1]))
assert len(rows) == 21 and set(coverage) == open_ids and set(coverage.values()) == {1}
dispositions = Counter(re.search(r'^disposition: (.*)$', new[number], re.M)[1] for number in open_ids)
assert dispositions == {'actionable': 3, 'blocked': 6, 'awaiting-decision': 8,
                        'accepted-limitation': 3, 'uncertain': 3}
for disposition, count in {'done': 634, **dispositions}.items():
    assert f'| {disposition} | {count} |' in backlog
print('PASS: 657 IDs, exactly five closures, 652 other bodies unchanged, all old selected fields preserved')
print('PASS: 634 done / 23 open, 21 maintenance rows cover every open ID exactly once')

checkouts = {
    'Builds': '3d7f49a5430294da5bcd437cbdace85c62c42027',
    'EventStore': '36c78f3435d2d492a73f1b0707cf865c8735d740',
    'McpCli': '2d7311cbbcc592c0ea0348463037389251d1cb4b',
    'Memories': '72c36c7113c750c8b809f9d72b945a2a8c2d4555',
    'Platform': '8de90958bc41b7fd01b7ed22b8ab364447a800e4',
}
for name, oid in checkouts.items():
    assert git('-C', 'references/Hexalith.' + name, 'rev-parse', 'HEAD').strip() == oid.encode()
assert not git('diff', '--cached', ENTRY, '--', 'references/')
assert git('ls-tree', 'HEAD', 'references/') == git('ls-tree', ENTRY, 'references/')
print('PASS: five submodule checkout HEADs unchanged; index and committed gitlinks unchanged')
PY
```

The complete assertion block exited 0 in the initial run. Its checkout result is
restated below as the measured HEAD/gitlink preservation; those assertions do
not compare complete dependency content, untracked files or nested checkouts.
The output label in the reproducible block now states that narrower claim.

```text
PASS: unavailable original, sole creation, exact parent/endpoint, seven exact additions
PASS: original tooling diffs/blobs, source stories and superseded draft unchanged, generated tooling unchanged
PASS: 657 IDs, exactly five closures, 652 other bodies unchanged, all old selected fields preserved
PASS: 634 done / 23 open, 21 maintenance rows cover every open ID exactly once
PASS: five submodule checkout HEADs unchanged; index and committed gitlinks unchanged
```

```bash
python3 - <<'PY'
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

root = Path.cwd()
with tempfile.TemporaryDirectory(prefix='historical-guard-parent-mutation-') as directory:
    probe = Path(directory)
    for name in ['scripts/validate-changed-story-gitlinks.py', 'scripts/validate-story-gitlinks.py',
                 'tests/scripts/test_validate_changed_story_gitlinks.py']:
        target = probe / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(root / name, target)
    runner = probe / 'scripts/validate-changed-story-gitlinks.py'
    original = runner.read_text()
    exact = 'if parents_by_commit[creation] != [baseline]:'
    assert original.count(exact) == 1
    runner.write_text(original.replace(exact, 'if parents_by_commit[creation][0] != baseline:'))
    result = subprocess.run([
        sys.executable, str(probe / 'tests/scripts/test_validate_changed_story_gitlinks.py'),
        'ChangedStoryGitlinkTests.test_baseline_correction_refuses_original_creation_in_multi_parent_merge',
    ], cwd=probe, text=True, capture_output=True)
    print(result.stdout + result.stderr)
    assert result.returncode == 1 and 'AssertionError: 0 == 0' in result.stderr
    print('PASS: temporary first-parent-only mutation is rejected by the isolated multi-parent test')
PY
```

## Review and final committed-event verification

All three independent layers ran: Blind Hunter, Edge Case Hunter and Verification
Gap. The spec retains one triage row per finding, including both follow-up
findings. Reproduced issues in event lifetime, cross-directory identity and merge
creation are corrected. Missing overlap/addition/removal/merge cases, endpoint
wording, direct replay instructions and evidence qualification are corrected.
The pending-publication observation was rejected because it concerned this
in-review handoff. No findings were deferred and no unnumbered ledger item was
added.

Final parent verification on 2026-10-28:

- Production guard: **77/77**, exit 0 (9.723 seconds).
- Committed-event runner: **78/78**, exit 0 (13.703 seconds).
- Retained provenance/ledger assertion block: exit 0; 657 IDs, exactly five
  closures, 652 byte-identical other bodies, all original selected fields
  recoverable, 634 done/23 open and exact 21-row coverage.
- Retained isolated first-parent-only mutation control: exit 0. Its deliberately
  mutated copy fails the targeted regression with `AssertionError: 0 == 0`;
  production files remain unchanged.
- All eight intended files were inspected in a unified diff. No application,
  generated workflow, skill, source story 1.10/4.3, superseded draft or
  dependency pointer was staged. Local Markdown targets and whitespace pass.
- Pinned commitlint CLI/config **21.2.3** match package-lock and installed
  packages. Exact implementation message validation:

```text
fix(tooling): repair historical story audit provenance
found 0 problems, 0 warnings
```

The earlier pre-review staged snapshot
`880e1fac8e7cdb1a7f359fd545435d8c82223c75` passed the actual event runner with
two selected stories and zero failures. It is an unreferenced validation object,
not a branch update or final code acceptance; the final corrected committed
snapshot is replayed and recorded below before the authorized push.

### Corrected implementation commit replay

Implementation commit: `f1e3a079984f91ed9b7ce7324150d455db966182`.
The actual push-event runner passed on the committed implementation snapshot,
not on local edits. Event payload:

```json
{"before":"7ddb864cda5e942925b056391617eae4895c1253","after":"f1e3a079984f91ed9b7ce7324150d455db966182"}
```

Save that payload as `/tmp/tenants-historical-implementation-event.json` and run:

```bash
python3 scripts/validate-changed-story-gitlinks.py --event-name push --event-path /tmp/tenants-historical-implementation-event.json
```

Observed exit **0**: Story 5.4 validates at its exact historical endpoint with
seven declared additions; the completed repair spec validates at the new
implementation commit with no dependency pointer changes. Final line:

```text
Changed stories: 2; failed: 0
```

The retained provenance/ledger assertion block also passed after that commit,
including unchanged checked-out submodule HEADs and unchanged superproject
index/committed gitlinks. The source stories, superseded draft and generated
tooling remain byte-identical to entry. The fetch before publication showed
main and origin/main at the same entry revision; no rebase or dependency update
was needed.

This final evidence-only record uses the pinned 21.2.3 commitlint validation:

```text
docs: record final historical audit validation
found 0 problems, 0 warnings
```

The combined branch range receives the same committed-event replay and a final
Git pointer-scope check before the explicitly authorized push to main. The
published branch commit and push result are reported in the completion message.
