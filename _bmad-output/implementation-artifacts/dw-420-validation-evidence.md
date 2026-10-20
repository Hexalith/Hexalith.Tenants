# DW-420 validation evidence — 2026-10-20

Entry revision: `5f18497fa034d7cd9a45422a507eb0bbc9fc6ab6`, clean `main`.

## Scope and attribution

DW-420 describes the same legacy unchanged-pointer resolver defect already fixed by DW-656 in `948141b48aa29af67caa8531d821be205096c46d`. The current resolver verifies that a dependency checkout owns its directory before reading HEAD, otherwise uses the exact stage-zero mode-160000 index entry. Historical `--ref <commit>` audits read the named tree when the ref is not the literal `HEAD`; both the default and explicit `--ref HEAD` include the working tree. [DW-656 evidence](dw-656-validation-evidence.md) records the original implementation; this run reconciles the stale open record without changing production or test sources.

The existing real-CLI cases cover empty and deinitialized directories, both table/arrow claim forms, matching/stale/superproject-HEAD claims, a staged change that cancels the net baseline diff, and real `.git` directory/gitfile checkouts. They also distinguish a genuinely missing directory, ordinary non-gitlink paths and unresolved conflicts. The maintained fixtures construct actual gitlinks directly. An additional isolated overlay now stages a matching `.gitmodules` declaration, verifies its path with `git config`, and reruns all five focused methods successfully, including empty/deinitialized directories and both real-checkout forms. This directly covers the original declared-submodule condition without changing maintained tests or initializing workspace submodules. The overlay stages only within each temporary test repository and performs no dependency fetch.

## Commands and results

Run from the repository root:

| Command or isolated control | Result |
| --- | --- |
| `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 69/69 tests pass in 7.024 seconds, zero failures/skips. |
| `python3 /tmp/tenants-dw-420-ip7yk35h/qualify.py` | Exit 0; five focused methods pass unchanged and with a staged `.gitmodules` declaration; all three incorrect resolver controls and the counterfeit historical signature are rejected as specified below. |
| Historical resolver transplanted from `43786ea8c4a3cdd69562f7eec1109c1b891f6492` into an isolated current guard copy | Child exit 1; five methods run, nine assertion/subtest failures across empty-directory (six), staged-pointer (two), and deinitialized-checkout (one) cases. Valid targets are rejected and parent-HEAD claims incorrectly pass; retained expected diagnostics are also checked. |
| Replace only the index fallback with tree-at-HEAD resolution in an isolated copy | Child exit 1; exactly two staged-pointer assertions fail. The other four methods pass. |
| Force unconditional index resolution before checking a real checkout in an isolated copy | Child exit 1; exactly the checkout-returned-to-baseline method fails at its direct resolver assertion, before that method reaches its CLI cases or second checkout form. The other four methods pass. The unchanged current run reaches the subsequent CLI cases and both forms. |
| Constant `"f" * 40` fallback with the same nine method/subtest failures | Qualification refuses it with `missing parent-HEAD unsafe-pass signature (table)`: it lacks the actual unsafe parent-HEAD acceptance. Counts alone cannot pass this behavioral control. |
| `python3 /tmp/tenants-dw-420-ip7yk35h/integrity.py` | Exit 0; exact current closure fields/links and complete four-file scope pass; original fields/full unrelated blocks remain, and every open ID has exactly one index row. |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-420-uninitialized-gitlink-acceptance.md` | Exit 0; RESULT: PASS, no pointer changes. |
| `git diff --check`, plus direct whitespace and embedded-script source checks for both new documents | Exit 0; no whitespace errors; all three embedded scripts match their executed sources and compile. |

Expected child failures make the enclosing qualification succeed. The harness requires exact failure names/multiplicities, exit codes, test totals, final summaries and absence of errors/skips. For the historical replay it additionally checks both table/arrow unsafe parent-HEAD PASS verdicts and matching parent-pointer diagnostics for incorrectly rejected valid targets; an unrelated constant-pointer implementation is explicitly refused despite reproducing the same failure counts. It uses explicit runtime checks rather than Python assertions. Temporary copies contain every mutation; no workspace script, test, index or dependency checkout is replaced.

Qualified SHA-256 inputs:

- Guard: `beae32d1c74bd07127637e79e6c69f2a072f76236977d27109ccf17bf0088fe4`.
- Tests: `da4cf038fa1032f1df143f2a62a5907e4a5de5e52724b583798277d12449e9d4`.

### Reproduce the focused controls

Save the following as `qualify.py` in any writable temporary directory and run `python3 /absolute/path/qualify.py` from the repository root. It requires only Python 3, Git and the recorded repository history, checks the pinned inputs before running, and writes child logs plus `qualification.json` beside itself. The historical control transplants only `current_pointer`, retaining every other current guard behavior; it does not claim to execute the entire historical script.

```python
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import tempfile

ROOT = Path.cwd()
OUTPUT = Path(__file__).resolve().parent
SCRIPT = Path('scripts/validate-story-gitlinks.py')
TESTS = Path('tests/scripts/test_validate_story_gitlinks.py')
BEFORE_FIX = '43786ea8c4a3cdd69562f7eec1109c1b891f6492'
METHODS = [
    'test_explicit_commit_table_targets_are_checked_when_the_pointer_did_not_move',
    'test_default_head_empty_checkout_checks_unchanged_claims_against_the_gitlink',
    'test_default_head_empty_checkout_uses_the_staged_pointer_when_the_net_diff_is_empty',
    'test_default_head_own_checkouts_preserve_matching_claims_and_movement_refusals',
    'test_default_head_own_checkout_wins_over_the_index_when_it_returns_to_baseline',
]


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def run_case(name, source, expected_failures, declared=False, check_parent=False):
    with tempfile.TemporaryDirectory(prefix=f'dw-420-{name}-') as directory:
        root = Path(directory)
        (root / SCRIPT.parent).mkdir(parents=True)
        (root / TESTS.parent).mkdir(parents=True)
        (root / SCRIPT).write_text(source, encoding='utf-8')
        test_source = test_bytes.decode()
        if declared:
            anchor = '    def tearDown(self):'
            require(test_source.count(anchor) == 1, 'Declared fixture must match exactly once')
            declaration = '''        (self.repo / ".gitmodules").write_text(
            '[submodule "Dependency"]\\n\\tpath = references/Dependency\\n'
            '\\turl = https://example.invalid/dependency.git\\n', encoding="utf-8")
        self._git("add", ".gitmodules")
        self.assertEqual(self._git("config", "--file", ".gitmodules", "--get",
                                   "submodule.Dependency.path").stdout.strip(),
                         "references/Dependency")

'''
            test_source = test_source.replace(anchor, declaration + anchor, 1)
        (root / TESTS).write_text(test_source, encoding='utf-8')
        command = [sys.executable, str(root / TESTS), '-v', *[
            'GuardCommandIntegrationTests.' + method for method in METHODS
        ]]
        result = subprocess.run(command, capture_output=True, text=True, timeout=60)
        log = result.stdout + result.stderr
        (OUTPUT / f'{name}.log').write_text(log, encoding='utf-8')
        require(re.search(r'Ran 5 tests in', log), f'{name}: wrong count\n{log}')
        require(not re.search(r'^ERROR:|skipped=|errors=', log, re.M), f'{name}: error/skip\n{log}')
        failures = re.findall(r'^FAIL: (\w+) ', log, re.M)
        require(result.returncode == (1 if expected_failures else 0), f'{name}: wrong exit\n{log}')
        require(sorted(failures) == sorted(expected_failures), f'{name}: wrong failures\n{log}')
        summary = f'FAILED (failures={len(expected_failures)})' if expected_failures else 'OK'
        require(log.rstrip().endswith(summary), f'{name}: wrong summary\n{log}')
        if check_parent:
            # Counts alone also fit unrelated bad-pointer implementations.
            blocks = re.split(r'(?=^FAIL: )', log, flags=re.M)[1:]
            for form in ('table', 'arrow'):
                matching = [block for block in blocks if block.startswith('FAIL: ' + METHODS[1])
                            and f"(form='{form}'," in block.splitlines()[0]]
                accepted = [block for block in matching if 'AssertionError: 0 != 1 :' in block]
                rejected = [block for block in matching if 'AssertionError: 1 != 0 :' in block]
                require(len(accepted) == len(rejected) == 1,
                        f'{name}: missing parent-HEAD unsafe-pass signature ({form})')
                parent = re.search(r"claim='([0-9a-f]{40})'", accepted[0].splitlines()[0])[1]
                diagnostic = re.search(
                    r'\[MISSTATED\] references/Dependency is ([0-9a-f]{40}) in the tree, '
                    r'but the story states ([0-9a-f]{40})\.', rejected[0])
                require(diagnostic is not None and diagnostic[1] == parent and diagnostic[2] != parent,
                        f'{name}: wrong historical resolver diagnostic ({form})')
                require(f'Compared: HEAD ({parent[:7]}) + working tree' in accepted[0]
                        and '\nRESULT: PASS\n' in accepted[0] and '\nRESULT: FAIL\n' in rejected[0],
                        f'{name}: wrong historical CLI verdicts ({form})')
        return {'name': name, 'exit': result.returncode, 'tests': 5,
                'failures': failures, 'declared': declared, 'checked_parent_behavior': check_parent,
                'source_sha256': hashlib.sha256(source.encode()).hexdigest()}


for path, expected in (
    (SCRIPT, 'beae32d1c74bd07127637e79e6c69f2a072f76236977d27109ccf17bf0088fe4'),
    (TESTS, 'da4cf038fa1032f1df143f2a62a5907e4a5de5e52724b583798277d12449e9d4'),
):
    require(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected,
            f'Qualified input mismatch: {path}')
test_bytes = (ROOT / TESTS).read_bytes()
source = (ROOT / SCRIPT).read_text(encoding='utf-8')
historical = subprocess.check_output(['git', 'show', f'{BEFORE_FIX}:{SCRIPT}'], text=True)
# Isolate the previous resolver while retaining every other current protection.
start, end = 'def current_pointer(', '\ndef baseline_is_mid_story('
old_function = historical[historical.index(start):historical.index(end)]
old_resolver = source[:source.index(start)] + old_function + source[source.index(end):]
fallback = '        return indexed_pointer\n'
require(source.count(fallback) == 1, 'Fallback mutation must match exactly once')
head_fallback = source.replace(fallback, '        return current_pointer(path, "HEAD")\n', 1)
checkout = '        try:\n            checkout_root = run_git('
require(source.count(checkout) == 1, 'Checkout mutation must match exactly once')
index_only = source.replace(checkout, '        return indexed_pointer\n' + checkout, 1)
results = [run_case(name, contents, expected) for name, contents, expected in (
    ('current', source, []),
    ('head-instead-of-index', head_fallback, [METHODS[2]] * 2),
    ('index-only', index_only, [METHODS[4]]),
)]
results.append(run_case('declared-current', source, [], declared=True))
historical_failures = [METHODS[1]] * 6 + [METHODS[2]] * 2 + [METHODS[3]]
results.append(run_case('historical-resolver', old_resolver, historical_failures, check_parent=True))
counterfeit = source.replace(fallback, '        return "f" * 40\n', 1)
try:
    run_case('counterfeit-historical', counterfeit, historical_failures, check_parent=True)
except SystemExit as error:
    require(str(error) == 'counterfeit-historical: missing parent-HEAD unsafe-pass signature (table)',
            f'Wrong counterfeit refusal: {error}')
    results.append({'name': 'counterfeit-historical', 'rejected': str(error)})
else:
    raise SystemExit('Counterfeit historical signature was incorrectly accepted')
for path, expected in ((SCRIPT, source.encode()), (TESTS, test_bytes)):
    require((ROOT / path).read_bytes() == expected, f'Workspace input changed: {path}')
(OUTPUT / 'qualification.json').write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
for result in results:
    print(json.dumps(result))
```

## Ledger integrity and preservation

Only DW-420 closes. Its replaced fields remain verbatim under `pre-dw-420-*`; every unrelated block, including headings and line endings, is unchanged. The maintenance index removes exactly its one row and prepends the dated closure receipt. Prior summaries and inherited aggregate tables are retained as historical content; the new receipt records current totals: 656 IDs, 386 done/270 open across 242 exactly covering rows. Archive, sprint, production/tests, dependency pointers, staging and HEAD remain unchanged. The reproducible integrity script now also requires the complete changed/untracked path set to equal the four planned documents and validates every current DW-420 closure field exactly once against its expected value, with both resolution links required to exist.

The following check compares against the canonical entry revision, so it does not depend on the session's temporary entry snapshot. Save it as `integrity.py` anywhere and run it from the repository root:

```python
from pathlib import Path
import re
import subprocess

BASE = '5f18497fa034d7cd9a45422a507eb0bbc9fc6ab6'
FIELDS = {b'status', b'disposition', b'verification-date', b'verification',
          b'verification-files', b'evidence-kind', b'next-action'}

EXPECTED_FIELDS = {
    b'disposition': b'done',
    b'evidence-kind': b'qualified-existing-fix',
    b'next-action': b'Closed after DW-656 acceptance reconciliation; retain DW-419 and separately scoped CI, history-attribution and release work.',
    b'resolution-evidence': b'_bmad-output/implementation-artifacts/dw-420-validation-evidence.md',
    b'resolution-spec': b'_bmad-output/implementation-artifacts/spec-dw-420-uninitialized-gitlink-acceptance.md',
    b'status': b'done 2026-10-20',
    b'verification': b'DW-656 commit 948141b48aa29af67caa8531d821be205096c46d already verifies checkout ownership and resolves uninitialized/deinitialized unchanged legacy pointers from the stage-zero index, including staged net-zero changes. Fresh maintained guard tests pass 69/69. Five real-CLI focused cases pass; isolated prior-resolver replay produces nine assertion/subtest failures across three cases, HEAD-tree fallback fails two staged-pointer assertions, and unconditional-index resolution fails the checkout-precedence case. Independent review and preservation results are recorded in dw-420-validation-evidence.md.',
    b'verification-date': b'2026-10-20',
    b'verification-files': b'scripts/validate-story-gitlinks.py, tests/scripts/test_validate_story_gitlinks.py, _bmad-output/implementation-artifacts/dw-420-validation-evidence.md',
}

def require(condition, message):
    if not condition:
        raise SystemExit(message)


def original(path):
    return subprocess.check_output(['git', 'show', f'{BASE}:{path}'])


def records(data):
    pairs = [(match[1], match[0]) for match in re.finditer(
        rb'^### DW-(\d+):[^\n]*\n.*?(?=^### DW-|\Z)', data, re.M | re.S)]
    require(len(pairs) == len(dict(pairs)), 'Duplicate ledger IDs')
    return dict(pairs)


def remove_summary(data):
    title, summary, rest = data.split(b'\n\n', 2)
    require(summary.startswith(b'**DW-420 closure (2026-10-20):**'), 'Missing closure summary')
    return title + b'\n\n' + rest


ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
before_data, after_data = original(ledger), Path(ledger).read_bytes()
before, after = records(before_data), records(after_data)
require(set(before) == set(after) and len(after) == 656, 'IDs changed')
require({key for key in before if before[key] != after[key]} == {b'420'}, 'Unrelated record changed')
expected = [b'pre-dw-420-' + line if line.partition(b':')[0] in FIELDS else line
            for line in before[b'420'].splitlines(keepends=True)]
retained = [line for line in after[b'420'].splitlines(keepends=True)
            if line.partition(b':')[0] not in FIELDS | {b'resolution-evidence', b'resolution-spec'}]
require(retained == expected, 'Historical DW-420 fields changed')

for key, value in EXPECTED_FIELDS.items():
    actual = re.findall(rb'^' + re.escape(key) + rb': ([^\n]*)$', after[b'420'], re.M)
    require(actual == [value], f'Incorrect closure field: {key.decode()}')
for key in (b'resolution-evidence', b'resolution-spec'):
    require(Path(EXPECTED_FIELDS[key].decode()).is_file(), f'Missing closure link: {key.decode()}')

require(remove_summary(after_data).split(b'### DW-1:')[0] == before_data.split(b'### DW-1:')[0],
        'Ledger preamble changed')
open_ids = {key for key, value in after.items() if re.search(rb'^status: open\b', value, re.M)}
done_ids = {key for key, value in after.items() if re.search(rb'^status: done\b', value, re.M)}
require(len(open_ids) == 270 and len(done_ids) == 386 and open_ids | done_ids == set(after),
        'Incorrect statuses/counts')
index = 'docs/maintenance-backlog.md'
old_index, new_index = original(index), Path(index).read_bytes()
removed = [line for line in old_index.splitlines(keepends=True) if line.startswith(b'| DW-420 |')]
require(len(removed) == 1, 'Expected one original index row')
require(remove_summary(new_index) == old_index.replace(removed[0], b'', 1), 'Unrelated index changed')
rows = [line for line in new_index.splitlines() if line.startswith(b'| DW-')]
covered = [key for line in rows for key in re.findall(rb'DW-(\d+)', line.split(b'|')[1])]
require(len(rows) == 242 and len(covered) == len(set(covered)) and set(covered) == open_ids,
        'Incorrect index coverage')
subprocess.run(['git', 'diff', '--exit-code', BASE, '--', 'references/',
                '_bmad-output/implementation-artifacts/deferred-work-archive.md',
                '_bmad-output/implementation-artifacts/sprint-status.yaml',
                'scripts/validate-story-gitlinks.py', 'tests/scripts/test_validate_story_gitlinks.py'], check=True)
subprocess.run(['git', 'diff', '--cached', '--exit-code'], check=True)
require(subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() == BASE, 'HEAD moved')
expected_paths = {
    ledger, index,
    '_bmad-output/implementation-artifacts/dw-420-validation-evidence.md',
    '_bmad-output/implementation-artifacts/spec-dw-420-uninitialized-gitlink-acceptance.md',
}
tracked = subprocess.check_output(['git', 'diff', '--name-only', '-z', BASE]).decode().split('\0')
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z']).decode().split('\0')
require((set(tracked) | set(untracked)) - {''} == expected_paths, 'Unexpected changed-file scope')
print('PASS: exact closure fields and four-file scope; only DW-420 closes; historical fields preserved; 656 IDs / 386 done / 270 open / 242 rows')
```

## Review correction controls

The independent reviewer reproduced both original scripts and identified six evidence issues. The ref wording, declared-submodule qualification, direct-assertion attribution, historical behavioral oracle, complete file scope and exact closure metadata have been corrected. The [spec triage log](spec-dw-420-uninitialized-gitlink-acceptance.md#review-triage-log) records every finding. No application/runtime defect was found and no finding is deferred.

`python3 /tmp/tenants-dw-420-ip7yk35h/review_controls.py` exits 0. A temporary local clone with the four documents passes; an unrelated tracked `AGENTS.md` edit fails the scope check, and removing each of four required closure fields fails its matching metadata check. The restored clone passes. This never edits the workspace or loads any skill from the clone. Save the following beside the preceding `integrity.py` to reproduce from the repository root:

```python
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path.cwd()
RUN = Path(__file__).resolve().parent
FILES = [
    '_bmad-output/implementation-artifacts/deferred-work.md',
    'docs/maintenance-backlog.md',
    '_bmad-output/implementation-artifacts/dw-420-validation-evidence.md',
    '_bmad-output/implementation-artifacts/spec-dw-420-uninitialized-gitlink-acceptance.md',
]


def check(root, expected=None):
    result = subprocess.run([sys.executable, str(RUN / 'integrity.py')], cwd=root,
                            capture_output=True, text=True, timeout=30)
    if expected is None:
        if result.returncode != 0:
            raise SystemExit(result.stdout + result.stderr)
    elif result.returncode != 1 or result.stderr.strip() != expected:
        raise SystemExit(f'Wrong refusal: {result.returncode}: {result.stdout}{result.stderr}')


with tempfile.TemporaryDirectory(prefix='dw-420-review-controls-') as directory:
    clone = Path(directory) / 'repo'
    subprocess.run(['git', 'clone', '--quiet', '--local', '--no-hardlinks', '--no-checkout',
                    str(ROOT), str(clone)], check=True)
    subprocess.run(['git', '-C', str(clone), 'checkout', '--quiet', '--detach',
                    '5f18497fa034d7cd9a45422a507eb0bbc9fc6ab6'], check=True)
    for name in FILES:
        destination = clone / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, destination)
    check(clone)
    agents = clone / 'AGENTS.md'
    original = agents.read_bytes()
    agents.write_bytes(original + b'\nDeliberate out-of-scope corruption.\n')
    check(clone, 'Unexpected changed-file scope')
    agents.write_bytes(original)
    ledger = clone / FILES[0]
    original = ledger.read_bytes()
    start, end = original.index(b'### DW-420:'), original.index(b'### DW-421:')
    block = original[start:end]
    for key in (b'verification-date', b'verification-files', b'resolution-evidence', b'resolution-spec'):
        lines = block.splitlines(keepends=True)
        removed = [line for line in lines if line.startswith(key + b':')]
        if len(removed) != 1:
            raise SystemExit(f'Bad corruption fixture: {key!r}')
        changed = block.replace(removed[0], b'', 1)
        ledger.write_bytes(original[:start] + changed + original[end:])
        check(clone, 'Incorrect closure field: ' + key.decode())
    ledger.write_bytes(original)
    check(clone)
print('PASS: unrelated AGENTS.md edit and four missing closure fields refused; restored clone passes')
```

During development of the declared-fixture overlay, the first qualification attempt exited 1 with `IndentationError: unexpected indent` before running that variant. Its temporary indentation was corrected; the final ordinary and declared variants each pass all five methods. The failed attempt supplies no acceptance evidence.

## Review and limits

The independent correction recheck extracted all three embedded scripts, confirmed exact source matches and reran each successfully (exit 0). All six findings are resolved, none are deferred, and no concrete issue remains. The owning spec is finalized as done. No application/.NET-build, distributed-runtime, CI-enforcement or release-readiness qualification is claimed. No application or test-source changes are needed, so no Aspire topology was started for this documentation reconciliation. DW-419, historical story attribution, cross-repository/CI work and existing release holds remain separately scoped. Changes remain uncommitted under the user-provided Git safeguards.
