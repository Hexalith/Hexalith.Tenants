# DW-431 validation evidence — 2026-10-20

Entry revision: `569e80e7364b57c92ffdd80519e58fc47bc8b096`, clean `main`. DW-431 is the historical-attribution prerequisite selected for the first queued CI gate bundle DW-437/DW-451/DW-534; that bundle remains open.

The repair records Story 5.2's historical endpoint and binds its four existing dependency declarations to exact closure targets. It does not change the guard or application behavior. The historical guard validates the endpoint snapshot, while the full Git chain below supplies attribution. Independent commits interleaved with story work are not represented as Story-Owned Gitlink Commits.

## Boundaries and commands

- True start: `fc147e3efab879f0d5f8131a4ebfd178bf3a113c`. `git rev-parse f17027ac24a9af28f2e87d05951040be3be5db80^` returns this exact ID.
- Initial implementation: `f17027ac24a9af28f2e87d05951040be3be5db80`. `git diff --raw --no-abbrev fc147e3efab879f0d5f8131a4ebfd178bf3a113c f17027ac24a9af28f2e87d05951040be3be5db80 -- references/` exits 0 with empty output.
- Final historical endpoint: `33d6dcca278952eaf65764e24b21000a9baa32d8`. `git log --format=%H 569e80e7364b57c92ffdd80519e58fc47bc8b096 -- _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md` identified this as its last change before this repair. Inspection of the commit's files also confirms story spec, sprint status and tests, plus two carried pointers.
- Enumerate complete pointer history: `git log --reverse --no-abbrev --raw --format='%H %s' fc147e3efab879f0d5f8131a4ebfd178bf3a113c..33d6dcca278952eaf65764e24b21000a9baa32d8 -- references/`. Exactly seven pointer-bearing commits contain 15 changes. The ordered old/new chain matches the true-start tree and final tree for every path.
- Audit the corrected final snapshot: `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md --ref 33d6dcca278952eaf65764e24b21000a9baa32d8` exits 0, four declared exact targets, RESULT: PASS.
- The same command without `--ref` exits 1 at entry HEAD: nine changed dependencies, five undeclared later dependencies and four mismatched historical targets, RESULT: FAIL. This is expected: current dependencies are outside the historical endpoint. No dependencies are reverted or newly attributed to the old story.
- With `--ref f17027ac24a9af28f2e87d05951040be3be5db80` the corrected document exits 1 with four MISSTATED closure targets. Its initial zero-pointer claim is established by the raw Git diff above. Replaying the entry document in a temporary copy confirms its original initial and closure guard PASS results (exit 0 each); these older PASS observations remain historical.

## Canonical commit attribution

The two commits containing Story 5.2 implementation/spec changes also carried dependency changes; this records what landed without asserting those pointer updates were required by product behavior. Five independent commits are separately identified. No historical commit is rewritten.

### `8431d9926ed655d32396f1f68973e0573ccc38e2`

feat(audit): update status and resolution for Story 5.1 and mark tenant audit trail as done. **Attribution:** Independent work; not Story 5.2 implementation.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.Builds` | `754d2b4b6615c5004606ba837dd9c925c57e8d14` | `0734e3177d82e42089547497bf961c6b7c478bbd` |
| `references/Hexalith.EventStore` | `04682eac0464b6b18bc02a39187f244fec3ab9bd` | `8ac62359c1ffdd520488ecd03bd9689c217e3788` |
| `references/Hexalith.FrontComposer` | `2e33757e191e91b9369ceeb1b25fa568b1e57c3b` | `05d122005d328ec8f15ecd276058be29730288c2` |

### `04f68c60da0a85ec066d68b80aecb9b4417f586d`

feat: update submodule references and add tenants bootstrap acceptance artifacts. **Attribution:** Independent work; not Story 5.2 implementation.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.Builds` | `0734e3177d82e42089547497bf961c6b7c478bbd` | `2326f983bad14d5398ee55bf2bdf6c86b63c39ea` |
| `references/Hexalith.EventStore` | `8ac62359c1ffdd520488ecd03bd9689c217e3788` | `c8127b7ebf1ad5c94e6ac8ec0fa3afc8d7054abc` |

### `dfa78e1053c0d0d3a83ac5586b8204f133be18a9`

chore: update submodule references for Hexalith.EventStore and Hexalith.FrontComposer. **Attribution:** Independent work; not Story 5.2 implementation.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `c8127b7ebf1ad5c94e6ac8ec0fa3afc8d7054abc` | `4fafcb953c3764a370499884ed2b926464806a19` |
| `references/Hexalith.FrontComposer` | `05d122005d328ec8f15ecd276058be29730288c2` | `07bfc221c97b30a7e58ac1d24ea347cc148760f8` |

### `3ba48896e69fd11607add35f09a485876e575127`

feat(tests): enhance tenant audit and membership lookup tests. **Attribution:** Story 5.2 implementation/review, carrying two pointers.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `4fafcb953c3764a370499884ed2b926464806a19` | `1cc6b44b4a71950fbd8f92469bebad3b0c9c05c5` |
| `references/Hexalith.FrontComposer` | `07bfc221c97b30a7e58ac1d24ea347cc148760f8` | `812de2fefc56950b26beb4babc6905cd08c2bc3e` |

### `45be8deb28fcb84c56942a31f0f48cc0a5d2eadb`

chore: update submodule references for Hexalith.Builds, Hexalith.Commons, and Hexalith.EventStore. **Attribution:** Independent work; not Story 5.2 implementation.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.Builds` | `2326f983bad14d5398ee55bf2bdf6c86b63c39ea` | `0610f7837221c9859e280f06f333d7452a40f5b1` |
| `references/Hexalith.Commons` | `37455bbb258d30e3ee86aa4b09769ae6e5638f9f` | `53f7961b517becde5b84ed4d20fe696b849b5cd9` |
| `references/Hexalith.EventStore` | `1cc6b44b4a71950fbd8f92469bebad3b0c9c05c5` | `68492519b868899e6ab6bf64931f19f0cb1ca6a7` |

### `40de0a4e1a545c32a942939582f6484474927469`

chore: update Hexalith.EventStore submodule reference. **Attribution:** Independent work; not Story 5.2 implementation.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `68492519b868899e6ab6bf64931f19f0cb1ca6a7` | `410b8545576c70a56680e9129d4e3e38d769d8bc` |

### `33d6dcca278952eaf65764e24b21000a9baa32d8`

feat(audit): update sprint status and un-defer CI gate for scoped audit evidence. **Attribution:** Story 5.2 final spec/status/test closure, carrying two pointers.

| Dependency | Before | After |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `410b8545576c70a56680e9129d4e3e38d769d8bc` | `e29b44a2d185b01ddafe534672153308285b4444` |
| `references/Hexalith.FrontComposer` | `812de2fefc56950b26beb4babc6905cd08c2bc3e` | `805a261bc2f4fbcf038f30919601d199fd78f1b5` |

## Verification and negative controls

`python3 /tmp/dw-431/check-history.py` independently checks canonical revisions, initial parent and empty diff, exact table-to-tree equality, parsed full guard targets, frozen-intent and other historical-content preservation, complete ordered seven-commit chain, and actual guard invocation results. It fails on any mismatch. Its temporary-spec controls make no working-tree or dependency changes:

| Case | Expected and observed |
| --- | --- |
| Corrected final closure | exit 0 / PASS |
| Moving HEAD | exit 1 / undeclared and mismatched targets |
| Initial endpoint with closure-target document | exit 1 / mismatched targets |
| Each of four targets replaced with its baseline pointer | four separate exit 1 / matching MISSTATED diagnostics |
| Each of four path declarations removed | four separate exit 1 / matching UNDECLARED diagnostics |
| Entry document at initial and final endpoints | two exit 0 / PASS controls |

The entry document already passed its declaration-only final audit. The repair adds exact target qualification and complete attribution; it does not claim a newly fixed production defect. The maintained command `python3 tests/scripts/test_validate_story_gitlinks.py` passes 77/77 with zero failures/errors. No new permanent test is added for this documentation repair.

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-431-historical-story-gitlink-attribution.md` passes with no pointer movement. `git diff --check` and exact selected-record/index preservation are checked again after review. Application builds, Aspire and browser runs are outside this documentation change's validation scope.

Only DW-431 closes. Original fields remain under `pre-dw-431-*`; unrelated records, all historical findings, archive, sprint, release holds and dependency pointers remain preserved. Expected inventory after closure: 656 IDs, 391 done/265 open, 237 maintenance rows covering every open ID exactly once. The separate changed-story CI gate remains open.

## Independent review and reproducible checks

The blind review returned five findings. The missing reusable control/preservation recipes and unclear current-document semantics were confirmed and corrected; file-level attribution evidence was added as a simple clarification. The completion-metadata observation was a pending workflow finalization step, completed below. No finding requires deferred work.

**Comparison semantics:** `--ref` selects the compared Git tree. The guard always reads the supplied Markdown file from disk. The passing command above checks today's repaired document against the historical closure tree; it does not claim the document stored in that historical commit already contained today's binding table. The temporary entry-document controls explicitly use `git show` to retrieve the older content.

**Attribution evidence:** `git show --format= --name-only <commit>` was inspected for each pointer-bearing commit. For `8431d9926ed655d32396f1f68973e0573ccc38e2`, the only non-gitlink changes are the deferred ledger and sprint status; their actual diff closes Story 5.1 while leaving Story 5.2 in review. For `04f68c60da0a85ec066d68b80aecb9b4417f586d`, the only non-gitlink changes are `tests/tenants-bootstrap-acceptance.{json,md}` under implementation artifacts. Commits `dfa78e1053c0d0d3a83ac5586b8204f133be18a9`, `45be8deb28fcb84c56942a31f0f48cc0a5d2eadb` and `40de0a4e1a545c32a942939582f6484474927469` change only the gitlinks shown above. Conversely, `3ba48896e69fd11607add35f09a485876e575127` changes the Story 5.2 spec, audit UI and tests, while `33d6dcca278952eaf65764e24b21000a9baa32d8` changes that spec, sprint status and workspace/lookup tests. This supports the recorded distinction between story-carried and independent commits without inferring that a pointer change was product-required.

Run both recipes from the repository root. They use only the standard library, the tracked guard and existing Git objects. Temporary files are removed automatically. A missing object, unexpected guard result or preservation mismatch makes the command fail.

```python
from pathlib import Path
import re, subprocess, tempfile

story = Path('_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md')
endpoint = '33d6dcca278952eaf65764e24b21000a9baa32d8'
text = story.read_text()
rows = re.findall(r'^\| `(references/[^`]+)` \| `([0-9a-f]{40})` \| `([0-9a-f]{40})` \|$', text, re.M)
if len(rows) != 4:
    raise RuntimeError('Expected four exact target rows')

def check(path, code, marker):
    result = subprocess.run(['python3', 'scripts/validate-story-gitlinks.py', str(path), '--ref', endpoint], capture_output=True, text=True)
    if result.returncode != code or marker not in result.stdout:
        raise RuntimeError(result.stdout + result.stderr)

check(story, 0, 'RESULT: PASS')
with tempfile.TemporaryDirectory() as directory:
    candidate = Path(directory) / story.name
    for path, before, after in rows:
        row = f'| `{path}` | `{before}` | `{after}` |'
        candidate.write_text(text.replace(row, f'| `{path}` | `{before}` | `{before}` |'))
        check(candidate, 1, f'[MISSTATED] {path}')
        candidate.write_text(text.replace(f'- `{path}`\n', ''))
        check(candidate, 1, f'[UNDECLARED] {path}')
print('PASS: final historical snapshot and eight independent negative controls')
```

```python
from collections import Counter
from pathlib import Path
import re, subprocess

entry = '569e80e7364b57c92ffdd80519e58fc47bc8b096'
ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
index = 'docs/maintenance-backlog.md'
story = '_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md'

def require(ok, detail):
    if not ok:
        raise RuntimeError(detail)

def old(path):
    return subprocess.check_output(['git', 'show', f'{entry}:{path}']).decode()

def current(path):
    return Path(path).read_bytes().decode()

def records(text):
    matches = list(re.finditer(r'^### DW-(\d+):.*?(?=^### DW-|\Z)', text, re.M | re.S))
    result = {int(m.group(1)): m.group() for m in matches}
    require(len(matches) == len(result), 'Duplicate DW IDs')
    return result

a, b = records(old(ledger)), records(current(ledger))
require(set(a) == set(b) == set(range(1, 657)), 'Changed ID inventory')
require([key for key in a if a[key] != b[key]] == [431], 'Unrelated record changed')
for line in a[431].splitlines():
    require(line in b[431].splitlines() or 'pre-dw-431-' + line in b[431].splitlines(), 'Lost original field: ' + line)
counts = Counter(re.search(r'^status: (\S+)', body, re.M).group(1) for body in b.values())
require(counts == {'done': 391, 'open': 265}, 'Wrong record counts')
open_ids = {key for key, body in b.items() if re.search(r'^status: open$', body, re.M)}
rows = lambda text: [line for line in text.splitlines() if line.startswith('| DW-')]
prior, final = rows(old(index)), rows(current(index))
require(final == [line for line in prior if not line.startswith('| DW-431 |')], 'Unrelated maintenance row changed')
covered = [int(key) for line in final for key in re.findall(r'DW-(\d+)', line.split('|')[1])]
require(len(final) == 237 and set(covered) == open_ids and len(covered) == len(set(covered)), 'Invalid index coverage')
for path in (ledger, index):
    text = current(path)
    title, summary, rest = text.split('\n\n', 2)
    require(summary.startswith('**DW-431 closure'), 'Unexpected preamble')
    restored = title + '\n\n' + rest
    if path == ledger:
        restored = restored.replace(b[431], a[431], 1)
    else:
        removed = next(line for line in prior if line.startswith('| DW-431 |'))
        following = prior[prior.index(removed) + 1]
        restored = restored.replace(following, removed + '\n' + following, 1)
    require(restored == old(path), 'Unrelated preamble or other content changed')
frozen = lambda text: re.search(r'<frozen-after-approval\b.*?</frozen-after-approval>', text, re.S).group()
require(frozen(old(story)) == frozen(current(story)), 'Frozen intent changed')
text = current(story)
addition = text[text.index('\n**Historical endpoint correction'):text.index('\n## Verification\n')]
require(text.replace(addition, '', 1) == old(story), 'Other historical content changed')
for name in ('deferred-work-archive.md', 'sprint-status.yaml', 'release-readiness.md'):
    path = '_bmad-output/implementation-artifacts/' + name
    require(old(path) == current(path), 'Protected file changed: ' + name)
require(not subprocess.check_output(['git', 'diff', entry, '--', 'references/']), 'Dependency movement')
print('PASS: exact preservation; 656 IDs, 391 done/265 open, 237 exact-covering rows')
```

Both recorded recipes pass after the review corrections. The unchanged maintained guard lane remains 77/77. The final own-story gitlink guard passes with no dependency movement, and `git diff --check` passes. The new resolution spec is finalized as done with all five findings triaged; no new deferred entry is added.

The exact complete commit message `docs(tooling): bind Story 5.2 gitlink audit to its historical closure` passed `node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/dw-431/commit-message.txt --verbose` with exit 0, zero problems and zero warnings. The installed CLI and package-lock pin both identify version 21.2.3; validation output is retained in `/tmp/dw-431/commitlint.log`.

**Independent correction recheck:** The same reviewer inspected all five corrections and independently executed both recorded recipes successfully, confirming the snapshot/negative controls and exact 656-ID/391-done/265-open/237-row preservation. All five findings are addressed; no outstanding correction or new deferral remains.
