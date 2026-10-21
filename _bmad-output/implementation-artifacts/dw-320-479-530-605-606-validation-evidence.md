# DW-320 / DW-479 / DW-530 / DW-605 / DW-606 validation

Date: 2026-10-21. Clean entry on main: `6cd7521fc0732823490c2c2f18da0120f77991b5`.

## Changes and existing resolutions

DW-320 removes the unused lifecycle-specific `DuplicatePrevented` factory, terminal-ownership arm and display-symbol arm. Production duplicate refusal already uses `BlockedWithTracking`, preserving its preview or active attempt. Updated tests exercise that live path for previews, accepted attempts and projection-pending attempts, retaining exact identity and AuditPending evidence. The two unused EN/FR lifecycle state labels are removed; live duplicate-refusal resources remain. Shared duplicate state, member-removal handling, audit mappings and the still-used lifecycle refusal resources remain intact. Existing rendered tests cover repeated submits during preflight and dispatch, cancellation and remount with the same message ID.

DW-479, DW-530 and DW-605 are stale restatements. Commit `5f41f089157ef077c5145f34a260004cafa5349c` already deleted `TenantCreateCommandModels.cs` and split its fourteen public types into matching files. A source check extracts the fourteen original declarations from that commit's parent and verifies each current file declares exactly its corresponding public type. No model split is attributed to this change.

DW-606's two working files already contain only CRLF. Their committed blobs correctly use normalized LF, but the old unspecified `eol` could produce LF on checkout. Two scoped `.gitattributes` rules now require CRLF regardless of local defaults. A temporary index and empty checkout test both entry and changed attributes under `core.autocrlf=false` and `core.eol=lf`: entry produces LF, changed attributes produce 766/766 and 206/206 CRLF newlines. Both checkout variants preserve identical normalized contents. The real index is untouched by this check. Source-check results are in the companion `dw-320-479-530-605-606-source-checks.json`.

## Runtime and build

The Aspire skill was used to inspect the existing AppHost:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Resources report Running/Healthy. Raw configuration output remains outside the repository under `/tmp/tenants-model-hygiene-aspire-*.json`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial build: zero warnings/errors, 1m33.38s (`/tmp/tenants-model-maintenance-build.log`). Final reviewed build: zero warnings/errors, 15.61s (`/tmp/tenants-model-maintenance-build-final.log`). The first review-test build used the nonexistent enum label `TenantCommandAuditState.Pending` and failed with CS0117; correcting it to `AuditPending` restored the build. That failure is retained in `/tmp/tenants-model-maintenance-build-review-initial.log`.

## Tests

Maintained project command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Focused invocations add `--filter-class '*CLASS' --minimum-expected-tests 1 --results-directory /tmp/tenants-model-maintenance/CLASS`.

| Class | Passed | Failed / skipped |
| --- | ---: | --- |
| TenantLifecycleCommandSnapshotTests | 79 | 0 / 0 |
| TenantCommandAuditStatesTests | 144 | 0 / 0 |
| TenantLifecycleActionAvailabilityTests | 113 | 0 / 0 |
| TenantRemoveMemberCommandSnapshotTests | 23 | 0 / 0 |
| TenantCommandFlowGuardTests | 24 | 0 / 0 |

Final focused total: 383/383. The final invocations use `/tmp/tenants-model-maintenance/accepted/CLASS` for their results directories; each includes `run.log` and `results.trx`. Initial 382/382 results remain at `/tmp/tenants-model-maintenance/CLASS`.

Full acceptance adds `--minimum-expected-tests 7115 --results-directory /tmp/tenants-model-maintenance/full`: 7115/7115 pass, zero failures/skips, 1m58.221s. Log: `/tmp/tenants-model-maintenance-full.log`; the initial TRX remains under that results directory. Initial full results are preserved separately from the final reviewed run; the review-build typo above is the only failed validation attempt.

Final reviewed full acceptance uses `--minimum-expected-tests 7116 --results-directory /tmp/tenants-model-maintenance/accepted/full`: 7116/7116 pass, zero failures/skips, 1m59.262s. Its `run.log` and `results.trx` are retained there. The ledger-validation JSON records these final counters. The companion `dw-320-479-530-605-606-run-manifest.json` preserves SHA-256 checksums of the tested assembly and each of the six final TRX files, with each run's identifier, timestamps and counters. The assembly checksum was captured after the accepted build and runs, before any further build.

## Review and closeout

The ledger check preserves all 657 IDs, every historical field value and every unrelated record. Only the selected five records close, leaving 435 done and 222 open. Removing their four maintenance rows leaves 202 rows covering each open ID exactly once. Live disposition totals now match the ledger (435 done, 136 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 36 uncertain); the index's old 410/161 and 247/224 aggregate counters were stale. Prior closure notes remain untouched.

`git -c core.safecrlf=false diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-320-479-530-605-606-command-model-maintenance.md` pass. Independent review found five small gaps, all patched: nondefault audit retention coverage, two unused state labels, reproducible source checks, reproducible ledger checks and retained run provenance. The final independent recheck clears all five, verifies assembly/TRX hashes and run identities/times/counters, and finds no introduced defect. No new finding is deferred. This maintenance change does not claim new browser, deployment or release-readiness evidence.

## Commit validation and synchronization

The installed commitlint CLI matches the lockfile-pinned version 21.2.3. The exact full message, including its final newline, is stored in `/tmp/tenants-model-maintenance-commit-message.txt`:

```text
refactor: complete five deferred source maintenance records
```

```bash
node_modules/.bin/commitlint --verbose < /tmp/tenants-model-maintenance-commit-message.txt
git fetch origin main
git rev-list --left-right --count HEAD...origin/main
```

Commitlint exits 0 with zero problems/warnings; successful output is retained in `/tmp/tenants-model-maintenance-commitlint.log`. Fetch succeeds and divergence is `0 0`, with remote main at the entry baseline above. Staging and commit follow independent review; main will then be pushed as requested.

## Reproduce the source and ledger qualification

The following is the complete one-off checker invoked as `python3 /tmp/tenants-model-maintenance-verify.py` from the repository root (exit 0). Copy it to that temporary path to repeat the checks. It uses an isolated index and temporary checkout; it leaves the real index and working sources intact.

```python
import collections
import os
from pathlib import Path
import re
import subprocess
import tempfile

baseline = '6cd7521fc0732823490c2c2f18da0120f77991b5'
split_commit = '5f41f089157ef077c5145f34a260004cafa5349c'
selected = {320, 479, 530, 605, 606}

def git(*args):
    return subprocess.check_output(['git', *args], text=True)

def records(text):
    return {int(m[1]): m[0] for m in re.finditer(
        r'^### DW-(\d+):.*?(?=^### DW-|\Z)', text, re.M | re.S)}

old_model = 'src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs'
declaration = r'^public (?:sealed )?(?:record|enum|class) (\w+)'
names = re.findall(declaration, git('show', split_commit + '^:' + old_model), re.M)
assert len(names) == 14 and not Path(old_model).exists()
for name in names:
    assert re.findall(declaration, Path(old_model).with_name(name + '.cs').read_text(), re.M) == [name]

paths = [
    'src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs',
    'src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs',
]
gitdir = git('rev-parse', '--absolute-git-dir').strip()
for changed in [False, True]:
    with tempfile.TemporaryDirectory(prefix='tenants-model-checkout-') as directory:
        env = dict(os.environ, GIT_INDEX_FILE=directory + '/index')
        subprocess.run(['git', 'read-tree', baseline], env=env, check=True)
        if changed:
            subprocess.run(['git', 'add', '.gitattributes'], env=env, check=True)
        checkout = Path(directory) / 'checkout'
        checkout.mkdir()
        subprocess.run([
            'git', '--git-dir=' + gitdir, '--work-tree=' + str(checkout),
            '-c', 'core.autocrlf=false', '-c', 'core.eol=lf',
            'checkout-index', '--prefix=' + str(checkout) + '/', '--', '.gitattributes', *paths,
        ], env=env, check=True)
        for path, expected in zip(paths, [766, 206], strict=True):
            data = (checkout / path).read_bytes()
            assert data.count(b'\n') == expected
            assert data.count(b'\r\n') == (expected if changed else 0)
            assert data.replace(b'\r\n', b'\n') == Path(path).read_bytes().replace(b'\r\n', b'\n')

ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
before = records(git('show', baseline + ':' + ledger))
after = records(Path(ledger).read_text())
assert len(before) == len(after) == 657 and set(before) == set(after)
assert {number for number in before if before[number] != after[number]} == selected
mutable = {'status', 'disposition', 'verification-date', 'verification',
           'verification-files', 'evidence-kind', 'next-action'}
for number in selected:
    assert re.search(r'^status: open$', before[number], re.M)
    assert re.search(r'^status: done 2026-10-21$', after[number], re.M)
    for line in before[number].splitlines():
        if line.partition(':')[0] in mutable:
            assert 'pre-command-maintenance-' + line in after[number].splitlines()
        else:
            assert line in after[number].splitlines()

index_path = 'docs/maintenance-backlog.md'
old_rows = [line for line in git('show', baseline + ':' + index_path).splitlines() if line.startswith('| DW-')]
index = Path(index_path).read_text()
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
def row_ids(line):
    return {int(number) for number in re.findall(r'DW-(\d+)', line.split('|')[1])}
removed = [line for line in old_rows if row_ids(line) & selected]
assert len(removed) == 4 and all(row_ids(line) <= selected for line in removed)
assert rows == [line for line in old_rows if line not in removed]
open_ids = {number for number, body in after.items() if re.search(r'^status: open$', body, re.M)}
coverage = [number for line in rows for number in row_ids(line)]
assert len(rows) == 202 and len(coverage) == len(open_ids) == 222
assert set(coverage) == open_ids
counts = collections.Counter(
    'done' if number not in open_ids else re.search(r'^disposition: (.+)$', body, re.M)[1]
    for number, body in after.items())
assert counts == dict(done=435, actionable=136, blocked=15,
                      **{'awaiting-decision': 26, 'accepted-limitation': 9, 'uncertain': 36})
for disposition, count in counts.items():
    assert f'| {disposition} | {count} |' in index
assert '**Remaining: 222 open records across 202 index rows.**' in index
print('PASS: 14 named type files, old/new checkout controls, historical fields, 657 IDs and exact 222-ID/202-row coverage.')
```
