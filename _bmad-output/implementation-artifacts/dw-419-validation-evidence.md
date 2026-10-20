# DW-419 validation evidence — 2026-10-20

Entry revision: `7f1e449dbe411853e065c938ca75d29e5688e15a`. The working tree was clean on `main`. This is a Python tooling repair; application code and dependencies are outside its scope.

## Behavior and focused verification

Legacy baseline-to-tree audits now return exit 1 / `RESULT: FAIL` when the existing baseline inspection detects this story's artifacts. The diagnostic identifies the matched artifact paths, explicitly describes the filename-stem heuristic, explains that earlier pointer changes fall outside the audit and requires the verified true-start commit before rerunning. Declarations or matching targets cannot override this refusal. Failed baseline inspection also propagates to the existing failed-verdict handler. Recorded provenance retains its warning and all existing chain, target, endpoint and working-tree checks.

- Entry maintained lane: `python3 tests/scripts/test_validate_story_gitlinks.py` — exit 0, 69/69 tests.
- Initial corrected new tests against entry production (before review additions) — exit 1, 75 methods / 25 assertion or subtest failures: 24 unsafe CLI PASS results across three methods plus one swallowed-inspection-error failure. No errors or skips.
- Final repaired maintained lane: the same command — exit 0, 77/77, zero failures/skips.
- Eight new methods cover hidden earlier pointer movement, movement in the baseline commit, ordinary and associated spec artifacts, default HEAD, explicit `--ref HEAD`, canonical historical refs, declared/undeclared paths, corrected true-start audits, unrelated baseline artifacts, recorded-mode warning/PASS and continued owned-commit refusals, and helper/command-entry inspection errors. Canonical historical refusal refs are now distinct from HEAD after a later unrelated commit. The command-entry fault test injects a Git inspection error and calls `main()` in process, asserting its failed exit value, diagnostic and absence of PASS; the other CLI refusal cases run real subprocesses. A second recorded-mode method explicitly qualifies target, endpoint, worktree and chain refusals, plus a passing earlier recorded endpoint after later movement.

The first new-test run failed 28 assertions/subtests; three extra failures came from a fixture without the empty dependency directory needed for a default working-tree audit. Git correctly reported that missing directory as a removal. The fixture now creates the directory; the corrected entry run has exactly the 25 intended failures above. The original failed attempt provides no additional acceptance evidence.

## Reproducible isolated controls

From the repository root run:

```bash
python3 _bmad-output/implementation-artifacts/dw-419-check-variants.py --output-directory /tmp/dw-419-replay
```

The tracked harness runs eight methods against isolated copies, obtains entry production directly from the canonical revision above, requires exact failure multiplicities, validates summaries/exits, and rejects unexpected errors/skips. Workspace inputs are never replaced. Logs and `qualification.json` are written only to the specified directory. A previous receipt is invalidated before qualification starts, so a failed rerun cannot leave a stale success manifest. Result: exit 0.

| Variant | Expected and observed |
| --- | --- |
| Current production | Eight methods pass. |
| Entry production | Exactly 26 failures: 24 unsafe real-CLI PASS results, one command-entry inspection-error PASS and one helper error-containment assertion. |
| Reject mid-story baselines in both modes | Three failures: two recorded-provenance positive controls and the specific protection-diagnostic control. |
| Keep legacy warning-only behavior | Exactly 24 refusal subtests fail. |
| Swallow baseline inspection errors | Only the helper and command-entry inspection-error tests fail (two failures). |

## Ledger integrity

Only DW-419 and its maintenance row change. All seven replaced fields remain verbatim under `pre-dw-419-*`; the original finding, source, ownership/dependency metadata and related work remain. All 656 unique IDs remain: 387 done, 269 open, exactly covered once by 241 index rows. Prior receipts, unrelated record bodies/index text, archive, sprint and dependency pointers are preserved. The following executed check compares against the canonical entry revision and verifies the complete seven-file scope. Save it anywhere and run it from the repository root with Python 3.

```python
from pathlib import Path
import re
import subprocess

BASE = '7f1e449dbe411853e065c938ca75d29e5688e15a'
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
FIELDS = {b'status', b'disposition', b'verification-date', b'verification', b'verification-files', b'evidence-kind', b'next-action'}
EXPECTED_PATHS = {LEDGER, INDEX, 'scripts/validate-story-gitlinks.py', 'tests/scripts/test_validate_story_gitlinks.py',
                  '_bmad-output/implementation-artifacts/spec-dw-419-legacy-mid-story-baseline.md',
                  '_bmad-output/implementation-artifacts/dw-419-validation-evidence.md',
                  '_bmad-output/implementation-artifacts/dw-419-check-variants.py'}

def require(condition, message):
    if not condition:
        raise SystemExit(message)


def original(path):
    return subprocess.check_output(['git', 'show', f'{BASE}:{path}'])


def records(data):
    pairs = [(m[1], m[0]) for m in re.finditer(rb'^### DW-(\d+):[^\n]*\n.*?(?=^### DW-|\Z)', data, re.M | re.S)]
    require(len(pairs) == len(dict(pairs)), 'Duplicate DW IDs')
    return dict(pairs)


def without_receipt(data):
    title, receipt, rest = data.split(b'\n\n', 2)
    require(receipt.startswith(b'**DW-419 closure (2026-10-20):**'), 'Missing dated receipt')
    return title + b'\n\n' + rest


before_data, after_data = original(LEDGER), Path(LEDGER).read_bytes()
before, after = records(before_data), records(after_data)
require(set(before) == set(after) and len(after) == 656, 'DW ID inventory changed')
require({key for key in before if before[key] != after[key]} == {b'419'}, 'Unrelated record changed')
expected = [b'pre-dw-419-' + line if line.partition(b':')[0] in FIELDS else line
            for line in before[b'419'].splitlines()]
retained = [line for line in after[b'419'].splitlines()
            if line.partition(b':')[0] not in FIELDS | {b'resolution-evidence', b'resolution-spec'}]
require(expected == retained, 'Selected historical fields changed')
for key in FIELDS | {b'resolution-evidence', b'resolution-spec'}:
    values = re.findall(rb'^' + key + rb': ([^\n]+)$', after[b'419'], re.M)
    require(len(values) == 1, f'Missing or duplicate field {key!r}')
    if key.startswith(b'resolution-'):
        require(Path(values[0].decode()).is_file(), 'Missing resolution artifact')
require(without_receipt(after_data).split(b'### DW-1:')[0] == before_data.split(b'### DW-1:')[0], 'Preamble changed')
open_ids = {key for key, block in after.items() if re.search(rb'^status: open\b', block, re.M)}
done_ids = {key for key, block in after.items() if re.search(rb'^status: done\b', block, re.M)}
require(len(open_ids) == 269 and len(done_ids) == 387 and open_ids | done_ids == set(after), 'Incorrect counts')
old_index, new_index = original(INDEX), Path(INDEX).read_bytes()
removed = [line for line in old_index.splitlines(keepends=True) if line.startswith(b'| DW-419 |')]
require(len(removed) == 1 and without_receipt(new_index) == old_index.replace(removed[0], b'', 1), 'Unrelated index edit')
rows = [line for line in new_index.splitlines() if line.startswith(b'| DW-')]
covered = [key for line in rows for key in re.findall(rb'DW-(\d+)', line.split(b'|')[1])]
require(len(rows) == 241 and len(covered) == len(set(covered)) and set(covered) == open_ids, 'Incorrect index coverage')
subprocess.run(['git', 'diff', '--exit-code', BASE, '--', 'references/',
                '_bmad-output/implementation-artifacts/deferred-work-archive.md',
                '_bmad-output/implementation-artifacts/sprint-status.yaml'], check=True)
changed = subprocess.check_output(['git', 'diff', '--name-only', '-z', BASE]).decode().split('\0')
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z']).decode().split('\0')
require((set(changed) | set(untracked)) - {''} == EXPECTED_PATHS, 'Unexpected file scope')
print('PASS: only DW-419 closes; original fields preserved; 656 IDs / 387 done / 269 open / 241 index rows; exact seven-file scope')
```

## Runtime baseline and limits

Repository guidance required the Aspire baseline before code changes. `aspire ps --format Json --non-interactive` found no running AppHost. `aspire start --no-build --isolated --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` succeeded; `aspire describe` reported all 23 resources Running/Healthy. An error-level `aspire otel logs --limit 5` query returned five existing Memories errors involving HTTP 500 retries and `InitializeTenantRegistryActivity`. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` succeeded. Existing Debug assets were used; this is no fresh .NET-build, browser, CI-enforcement or distributed-runtime qualification.

The existing detector is deliberately retained: it checks whether paths touched by the baseline commit contain the story filename stem. This includes associated spec/evidence names, as required by the motivating historical Story 1.6 example. It is conservative, may match similar names, and does not discover every earlier story commit when the baseline itself touches no matching artifact. This repair closes the detected-condition bypass only. Historical artifacts, general ownership policy, CI enforcement and release holds remain separately scoped.

## Commit validation and review

`node_modules/.bin/commitlint --edit /tmp/tenants-dw-419/commit-message.txt --verbose` — exit 0 using installed pinned `@commitlint/cli@21.2.3`; exact full message:

```text
fix(tooling): reject legacy mid-story gitlink baselines
```

Successful output: `found 0 problems, 0 warnings`.

Independent Blind Hunter review found six concrete improvements; all are patched, with no deferred findings. The complete classifications and evidence are in the owning spec's Review Triage Log. Corrections add exact artifact diagnostics, command-entry inspection failure coverage, genuinely historical refusal refs, recorded-mode chain/target/endpoint/worktree coverage, stale replay-receipt invalidation, and completed review metadata. Original review arithmetic: 29,343 changed bytes / 1,024 = 28.66 KiB; N = min(floor(sqrt(28.66) + 1), 10) = 6.

An isolated failed-rerun control seeds an earlier `qualification.json`, copies the harness to `/tmp/tenants-dw-419/failed-rerun.py` and injects `raise SystemExit("injected rerun failure")` immediately before loading current production. The modified harness exits 1 with exactly that diagnostic and removes the old manifest. It never replaces any workspace source. The final tracked harness separately exits 0 and writes the current receipt. The first expanded harness run correctly refused a stale expected PASS count of 24: the added command-entry test contributes a 25th unsafe PASS diagnostic in entry production. The harness expectation was corrected to 25; exact failure counts are 26/3/24/2 for the four faulty variants above.

The initial preservation check caught one added blank line in DW-419, corrected before acceptance. The embedded source matches the executed preservation check; it passes with exact seven-file scope and original history intact. The final own-story gitlink audit and scoped whitespace checks pass. No remote publication or release dispatch is part of this work; the requested Git commit/push is separate from these acceptance checks.

The independent correction recheck confirmed all six findings resolved and found no remaining concrete problems. It independently reran 77/77 tests, the eight-method replay with exact 26/3/24/2 faulty-variant failures, whitespace checks and the embedded seven-file ledger integrity check; all passed.
