# DW-252 / DW-256 / DW-258 / DW-259 / DW-282 validation

Date: 2026-10-21. Entry revision: `c92cd4b15a80c3edb295093d3a1f426aa5a5636f`.

## Implementation and scope

Six internal `ApplyProjectionEvidence` wrappers had no production callers. Their removal leaves the authoritative status/provider paths intact. The unused create and metadata synchronous nudge wrappers are also removed. This is retirement of unused seams, not delivery of the separate create-notification integration in DW-200. That record remains open. Metadata status refresh continues to perform its existing authoritative projection read.

Four ordinary removal regressions now install a projection evidence provider and invoke the live authoritative nudge path; their identity, admission, tracker and activity-release assertions remain. The held-status theory for create/add/change/metadata advances the retained tracker using typed confirmation snapshots, then waits for the real retained-state subscription before acquiring a foreign lease. The removal receipt race similarly advances retained confirmation and invokes the existing private audit-query method through reflection. This qualifies receipt assembly with the substituted gateway without introducing a production ingress. All original terminal/late-status/receipt/foreign-lease assertions remain. These race cases are controlled retained-state tests, not browser or end-to-end concurrency evidence.

TenantsWorkspace caches its optional scoped composition instance during initialization and reuses it for command connectivity, initial authority, submit-time reauthorization and audit read availability. Connectivity and authority values remain evaluated at their existing call sites. The composition contract exposes no connectivity notification event; refresh/rerender observes current getters. The existing command-disconnection regression now qualifies disconnected → connected → disconnected across real Refresh button events. The authoritative-empty predicate was already shared; a source check verifies it remains exactly unchanged. DW-282 is the explicit duplicate of DW-258.

## Runtime baseline and build

The Aspire skill inspected the existing topology before implementation:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

All 23 resources reported Running/Healthy. Raw configuration is retained only in `/tmp/tenants-ui-cleanup-aspire-{ps,describe}.json`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial build passed with zero warnings/errors in 16.52s. Build after the expanded connectivity regression passed with zero warnings/errors in 14.49s. Logs: `/tmp/tenants-ui-cleanup-build.log` and `/tmp/tenants-ui-cleanup-build-final.log`.

## Tests

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Focused invocations add `--filter-class '*CLASS' --minimum-expected-tests 1 --results-directory /tmp/tenants-ui-cleanup/CLASS`.

| Class | Passed | Failed / skipped |
| --- | ---: | --- |
| RemoveTenantMemberFlowTests | 240 | 0 / 0 |
| TenantRetainedCommandFlowTests | 207 | 0 / 0 |
| TenantsWorkspaceTests (all partials) | 136 | 0 / 0 |

Focused total: 583/583. Logs: `/tmp/tenants-ui-cleanup-CLASS.log`; each results directory contains its TRX.

Full UI acceptance adds `--minimum-expected-tests 7116 --results-directory /tmp/tenants-ui-cleanup/full`: 7116/7116 pass, zero failures/skips, 1m48.258s. The accepted build of the final whitespace-normalized sources passed with zero warnings/errors in 14.18s. Logs: `/tmp/tenants-ui-cleanup-build-accepted.log` and `/tmp/tenants-ui-cleanup-full.log`. The companion run manifest records all four TRX identities, times, counters and hashes, plus the assembly hash for this accepted full run. Focused runs preceded the whitespace-only normalization and final rebuild; their TRX hashes are recorded without attributing the final assembly hash to those earlier runs.

## Ledger preservation and reproducibility

Only the selected five record bodies change. Original mutable fields are preserved under `pre-ui-composition-cleanup-` names; all other original fields and all 652 unrelated record bodies remain unchanged. Four maintenance rows close (DW-258/DW-282 share one). All 657 identifiers remain: 440 done, 217 open, with exactly one index-row occurrence per open identifier across 198 rows. No archive, sprint, dependency pointer, authorization policy or release setting changes.

`python3 /tmp/tenants-ui-cleanup-verify.py` passes. Its complete contents follow so the source and ledger audit can be reproduced from the repository root; its JSON output is committed alongside this record.

```python
from pathlib import Path
from collections import Counter
import json
import re
import subprocess

baseline = 'c92cd4b15a80c3edb295093d3a1f426aa5a5636f'
selected = {252, 256, 258, 259, 282}
ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
index_path = 'docs/maintenance-backlog.md'
def original(path):
    return subprocess.check_output(['git', 'show', baseline + ':' + path], text=True)
def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    headings = [int(re.match(r'### DW-(\d+)', b)[1]) for b in blocks]
    assert len(headings) == len(set(headings)), 'Duplicate deferred-work IDs'
    return {n: b.rstrip() for n, b in zip(headings, blocks, strict=True)}
# Duplicates preceding their original must fail, including colonless/numeric aliases.
for heading in ['### DW-1: Duplicate', '### DW-1 Duplicate without colon', '### DW-01: Numeric alias']:
    try:
        records(heading + '\nstatus: open\n### DW-1: Original\nstatus: open\n')
    except AssertionError:
        pass
    else:
        raise AssertionError('Duplicate-heading control unexpectedly passed: ' + heading)
before, after = records(original(ledger)), records(Path(ledger).read_text())
assert len(before) == len(after) == 657 and before.keys() == after.keys()
assert {n for n in before if before[n] != after[n]} == selected
mutable = {'status', 'disposition', 'verification-date', 'verification', 'verification-files', 'evidence-kind', 'next-action'}
for n in selected:
    assert re.search(r'^status: open$', before[n], re.M)
    assert re.search(r'^status: done 2026-10-21$', after[n], re.M)
    for line in before[n].splitlines():
        expected = 'pre-ui-composition-cleanup-' + line if line.partition(':')[0] in mutable else line
        assert expected in after[n].splitlines(), (n, expected)
def row_ids(line):
    return {int(n) for n in re.findall(r'DW-(\d+)', line.split('|')[1])}
old_rows = [line for line in original(index_path).splitlines() if line.startswith('| DW-')]
index = Path(index_path).read_text()
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
removed = [line for line in old_rows if row_ids(line) & selected]
assert len(removed) == 4 and all(row_ids(line) <= selected for line in removed)
assert rows == [line for line in old_rows if line not in removed]
open_ids = {n for n,b in after.items() if re.search(r'^status: open$', b, re.M)}
coverage = [n for row in rows for n in row_ids(row)]
assert len(rows) == 198 and len(coverage) == len(open_ids) == 217
assert set(coverage) == open_ids
counts = Counter('done' if n not in open_ids else re.search(r'^disposition: (.+)$', b, re.M)[1] for n,b in after.items())
assert counts == dict(done=440, actionable=131, blocked=15, **{'awaiting-decision':26, 'accepted-limitation':9, 'uncertain':36})
for disposition, count in counts.items():
    assert f'| {disposition} | {count} |' in index
assert '**Remaining: 217 open records across 198 index rows.**' in index
components = Path('src/Hexalith.Tenants.UI/Components')
assert not any('ApplyProjectionEvidence' in p.read_text() for p in components.rglob('*.razor'))
workspace = (components/'Pages/TenantsWorkspace.razor').read_text()
assert workspace.count('Services.GetService<ITenantsBffComposition>()') == 1
assert 'GetService(typeof(ITenantsBffComposition))' not in workspace
assert '=> _bffComposition?.IsCommandSurfaceConnected is true;' in workspace
for component in ['CreateTenantFlow.razor','Metadata/EditTenantMetadataFlow.razor']:
    assert 'internal void ApplySignalRNudge' not in (components/'Tenants'/component).read_text()
# The already-centralized authoritative-empty implementation remains byte-for-byte unchanged.
def empty_predicate(text):
    return text[text.index('    // The first-tenant exception'):text.index('    private bool IsCommandSurfaceConnected')]
assert empty_predicate(workspace) == empty_predicate(original(str(components/'Pages/TenantsWorkspace.razor')))
result = dict(baseline=baseline, selected=sorted(selected), total_records=657, open_records=217, done_records=440, index_rows=198, dispositions=dict(counts), result='pass')
Path('_bmad-output/implementation-artifacts/dw-252-256-258-259-282-ledger-validation.json').write_text(json.dumps(result, indent=2) + '\n')
print('PASS: six retired evidence wrappers, one composition resolution, unchanged empty predicate, preserved history and exact 657-record/217-open/198-row ledger coverage.')
```

## Independent review

Blind Hunter returned eight findings. The complete checked triage is retained in the owning spec. Corrections clarify the controlled retained-publication tests, pin retained terminal identity, prove valid-create enabled/disabled behavior and zero captured dispatch after disconnection, add audit-read connectivity transitions, and reject duplicate numeric ledger headings before dictionary construction. Metadata retirement and existing Refresh/rerender connectivity behavior have explicit dispositions in the selected records. No new finding is deferred; recheck and post-review validation follow before completion.

## Commit validation and synchronization

The installed commitlint CLI and lockfile both specify 21.2.3. The exact full candidate message, including its newline, is in `/tmp/tenants-ui-cleanup-commit-message.txt`:

```text
refactor(ui): complete five deferred composition cleanup items
```

```bash
node_modules/.bin/commitlint --verbose < /tmp/tenants-ui-cleanup-commit-message.txt
git fetch origin main
git rev-list --left-right --count HEAD...origin/main
```

Commitlint: zero problems/warnings; evidence is `/tmp/tenants-ui-cleanup-commitlint.log`. The initial fetched comparison reports 0/0 divergence. `git -c core.safecrlf=false diff --check` and the story gitlink guard both pass. The commit and push follow accepted review and final consistency checks.

## Post-review validation

The reviewed Debug source build passed with zero warnings/errors in 15.67s (`/tmp/tenants-ui-cleanup-build-review.log`). Repeating the three focused commands with results/log paths under `/tmp/tenants-ui-cleanup/review/CLASS` and `/tmp/tenants-ui-cleanup-review-CLASS.log` passes 584/584: removal 240, retained attempts 207, workspace 137, zero failures/skips. The extra workspace case is the read-connectivity transition regression. The duplicate-heading negative control and full ledger/source audit pass. The post-review full run uses minimum 7117 and the `review/full` results path; its result is recorded below before completion. Earlier passing run results remain above as historical evidence.

Independent recheck accepts findings 1–7 and finds one checker mismatch: colonless headings were parsed but excluded from uniqueness checking. The final checker derives both numeric IDs and record bodies from the same split blocks, and rejects duplicate normal, colonless and zero-padded numeric headings before dictionary construction. All three negative controls and the real ledger/source audit pass.

Final independent recheck clears all eight findings: embedded/executed checker contents match; normal, colonless and zero-padded duplicate controls reject; actual ledger/source audit passes and matches stored JSON. No remaining review finding and no new deferred item.

Final post-review full UI acceptance: 7117/7117 pass, zero failures/skips, 1m53.249s. Exact invocation is the maintained command above plus `--minimum-expected-tests 7117 --results-directory /tmp/tenants-ui-cleanup/review/full`. Log: `/tmp/tenants-ui-cleanup-review-full.log`; TRX: `/tmp/tenants-ui-cleanup/review/full/results.trx`. No source/test change occurred after the reviewed build. The final run manifest preserves all eight initial/review TRX hashes and counters, both captured full-run assembly identities, and reviewed source hashes; the reviewed assembly hash applies to all four review lanes.

Final fetched origin/main comparison remains 0/0 before commit. The exact unchanged candidate passes commitlint again with zero problems/warnings. All ledger/source, gitlink and whitespace checks pass. Independent review is clear; no new item is deferred.
