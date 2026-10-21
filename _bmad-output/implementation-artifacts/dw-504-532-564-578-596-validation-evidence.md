# DW-504 / DW-532 / DW-564 / DW-578 / DW-596 validation

Date: 2026-10-21. Entry revision: `092bcd0eafffb9e85b6c3506ee532f1340d6e87f`.

## Changes and evidence boundaries

- DW-504: the old June Story 5.6 record now clearly marks its tenant audit-proof linking claims as superseded by the current tenant correction specification. Current `CorrectionStartPanel` deliberately calls `WithCorrectiveProof(null)`; the confirmed snapshot sets `MissingSupport` and `ProofLink = null`. The independent global-administrator Story 5.7 path is distinguished. Historical acceptance and completion text remains intact.
- DW-532: the historical dependency specification distinguishes Python structural equality of parsed JSON rows from the independent catalog byte/hash/BOM/CRLF preservation checks, except the intended version substitution. The original ambiguous note and R7 remain intact; neither dependency audit is accepted by this correction.
- DW-564: production authentication guidance describes removal projection lag, token/circuit renewal and the absence of a bounded revocation guarantee. It explains that stale claims cannot override projected absence and supplies a deployment checklist requiring a global-administrator-only refusal. `TenantsGlobalAdministratorVerifier` still checks persisted membership. This closes documentation only: DW-562 and DW-617 remain open authority-policy work.
- DW-578: project context lists both AppHost.Tests and Server.Tests in blocking Tier 2, matching `.github/workflows/ci.yml`.
- DW-596: context distinguishes the committed historical EventStore 3.120.0 catalog from the current root-pinned 3.126.0 catalog. Builds `9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b` committed 3.120.0 and Tenants `b3878f1488d2a4a097bac2e5c233fa07da6eb2cf` adopted it. Current pinned Builds `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` selects 3.126.0. Historical 2026-10-10 tests and audit failures remain historical, and DW-579 remains open. No dependency, audit or release acceptance is inferred.

DW-558 was considered but excluded: its preserved paging baseline `23b2a7691ae667fd160a48a5d7293f3cf38d48fe` is not an ancestor of current main, and none of the four file snapshots reachable through its entry-revision `--follow` history records the valid broadened start as baseline_commit. A bounded annotation cannot pass the existing historical audit policy. Its original file and ledger record remain exactly unchanged, and its index row remains open. DW-564 supplies the fifth bounded documentation correction instead.

## Historical story validation

Initial default-HEAD audits of the two amended historical stories failed on later independent dependency pointer evolution: the old Story 5.6 reports nine undeclared pointers, and the dependency spec reports seven undeclared pointers plus one declared pointer. The paging story separately fails baseline ancestry. These initial failures are inherited provenance observations, not test regressions or reasons to alter dependencies.

The changed-story CI policy explicitly supports documentation repairs using `gitlink_audit_ref`. Independent investigation verified these historical endpoints, including unchanged baseline identity and the absence of omitted later story-associated gitlink commits:

| Story | Historical audit endpoint |
| --- | --- |
| `5-6-preview-and-confirm-correction-with-linked-proof.md` | `a34120b5a205e66260b1b59fa499d87c3511f17a` |
| `spec-eventstore-3-117-1.md` | `ef4b7f2f99b2a60910b94481a198f591c51bfe5c` |

The three story-validator commands below exit 0 with `RESULT: PASS` and no pointer changes in the compared range. The separate whitespace check exits 0 with no output:

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md --ref a34120b5a205e66260b1b59fa499d87c3511f17a
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md --ref ef4b7f2f99b2a60910b94481a198f591c51bfe5c
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-504-532-564-578-596-evidence-reconciliation.md
git -c core.safecrlf=false diff --check
```

The reproducible audit below additionally calls the CI runner's actual `audit_ref` policy on each amended story before commit. The full committed-event check will run after commit and before push.

## Scope and reproducible audit

This is documentation-only work. No application code, tests, workflow/configuration or dependency pointers change, so no application build or test run is claimed. Narrow validation checks the actual source/configuration facts, new relative links, exact historical-line preservation, and complete ledger/index consistency. The 657 original IDs remain: 445 done and 212 open, exactly covered by 193 index rows. Only five original record bodies change; their previous mutable fields are retained under `pre-documentation-reconciliation-` names. All other original fields and the 652 unrelated record bodies remain intact, along with archive, sprint, dependency and release state.

Save the following as `/tmp/tenants-doc-reconcile-verify.py` and run `python3 /tmp/tenants-doc-reconcile-verify.py` from the repository root. Its saved JSON result is committed alongside this evidence. The default mode compares the computed result with that saved JSON and never writes it. Use `--write-result` only to regenerate after an intentional correction; `--result-path` allows a separate control fixture. The script uses entry revision snapshots and current worktree facts; replay against this batch is supported, while later source/catalog changes can legitimately cause a mismatch. It also reproduces the excluded paging-story ancestry and four-snapshot history checks.

```python
from collections import Counter
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import re
import subprocess

baseline = '092bcd0eafffb9e85b6c3506ee532f1340d6e87f'
selected = {504, 532, 564, 578, 596}
artifacts = Path('_bmad-output/implementation-artifacts')
ledger = artifacts / 'deferred-work.md'
index = Path('docs/maintenance-backlog.md')
def original(path):
    return subprocess.check_output(['git', 'show', baseline + ':' + str(path)], text=True)
def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    ids = [int(re.match(r'### DW-(\d+)', block)[1]) for block in blocks]
    assert len(ids) == len(set(ids)), 'duplicate numeric ledger heading'
    return dict(zip(ids, (block.rstrip() for block in blocks), strict=True))
before, after = records(original(ledger)), records(ledger.read_text())
assert len(before) == len(after) == 657 and before.keys() == after.keys()
assert {n for n in before if before[n] != after[n]} == selected
mutable = {'location', 'status', 'disposition', 'verification-date', 'verification', 'verification-files', 'evidence-kind', 'next-action'}
for n in selected:
    assert re.search(r'^status: open$', before[n], re.M)
    assert re.search(r'^status: done 2026-10-21$', after[n], re.M)
    for line in before[n].splitlines():
        expected = 'pre-documentation-reconciliation-' + line if line.partition(':')[0] in mutable else line
        assert expected in after[n].splitlines(), (n, expected)
open_ids = {n for n, block in after.items() if re.search(r'^status: open$', block, re.M)}
assert {558, 562, 579, 617} <= open_ids

def row_ids(line):
    return {int(n) for n in re.findall(r'DW-(\d+)', line.split('|')[1])}
old_rows = [line for line in original(index).splitlines() if line.startswith('| DW-')]
rows = [line for line in index.read_text().splitlines() if line.startswith('| DW-')]
removed = [line for line in old_rows if row_ids(line) & selected]
assert len(removed) == 5 and all(row_ids(line) <= selected for line in removed)
assert rows == [line for line in old_rows if line not in removed]
coverage = [n for row in rows for n in row_ids(row)]
assert len(rows) == 193 and len(coverage) == len(set(coverage)) == len(open_ids) == 212
assert set(coverage) == open_ids
counts = Counter('done' if n not in open_ids else re.search(r'^disposition: (.+)$', block, re.M)[1] for n, block in after.items())
assert counts == dict(done=445, actionable=126, blocked=15, **{'awaiting-decision':26, 'accepted-limitation':9, 'uncertain':36})
for disposition, count in counts.items():
    assert f'| {disposition} | {count} |' in index.read_text()
assert '**Remaining: 212 open records across 193 index rows.**' in index.read_text()

# Historical repair excluded from this batch remains byte-identical.
paging = artifacts / 'spec-global-admin-projection-paging.md'
assert paging.read_bytes() == subprocess.check_output(['git', 'show', baseline + ':' + str(paging)])

paging_history = subprocess.check_output(['git', 'log', '--follow', '--format=%H', baseline, '--', str(paging)], text=True).splitlines()
assert len(paging_history) == 4
old_start = '23b2a7691ae667fd160a48a5d7293f3cf38d48fe'
broadened_start = '6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513'
assert subprocess.run(['git', 'merge-base', '--is-ancestor', old_start, baseline]).returncode == 1
for endpoint in [old_start, baseline]:
    subprocess.run(['git', 'merge-base', '--is-ancestor', broadened_start, endpoint], check=True)
for revision in paging_history:
    snapshot = subprocess.check_output(['git', 'show', revision + ':' + str(paging)], text=True)
    field = re.search(r'^baseline_commit: [\'"]?([0-9a-f]+)', snapshot, re.M)
    assert field is None or field[1] == old_start

# Existing historical acceptance and human intent remain intact.
for name in ['5-6-preview-and-confirm-correction-with-linked-proof.md', 'spec-eventstore-3-117-1.md']:
    path = artifacts / name
    old_lines = original(path).splitlines()
    iterator = iter(path.read_text().splitlines())
    assert all(any(candidate == line for candidate in iterator) for line in old_lines), name

# Reproduce the changed-story runner's historical-ref policy before committing.
spec = importlib.util.spec_from_file_location('changed_guard', 'scripts/validate-changed-story-gitlinks.py')
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
endpoints = {
    '5-6-preview-and-confirm-correction-with-linked-proof.md': 'a34120b5a205e66260b1b59fa499d87c3511f17a',
    'spec-eventstore-3-117-1.md': 'ef4b7f2f99b2a60910b94481a198f591c51bfe5c',
}
for name, endpoint in endpoints.items():
    path = str(artifacts / name)
    assert guard.audit_ref(Path(path).read_text(), {path}, path, baseline, baseline, False) == endpoint
    subprocess.run(['python3', 'scripts/validate-story-gitlinks.py', path, '--ref', endpoint], check=True, stdout=subprocess.DEVNULL)
subprocess.run(['python3', 'scripts/validate-story-gitlinks.py', str(artifacts / 'spec-dw-504-532-564-578-596-evidence-reconciliation.md')], check=True, stdout=subprocess.DEVNULL)

context = Path('_bmad-output/project-context.md').read_text()
ci = Path('.github/workflows/ci.yml').read_text()
assert 'CI Tier 2 blocking tests: `AppHost.Tests` and `Server.Tests`' in context
section = ci.split('integration-test-projects: |', 1)[1].split('aspire-test-project:', 1)[0]
assert 'tests/Hexalith.Tenants.AppHost.Tests' in section and 'tests/Hexalith.Tenants.Server.Tests' in section
catalog = Path('references/Hexalith.Builds/Props/Directory.Packages.props')
assert re.search(r'<HexalithEventStoreVersion\b[^>]*>3\.126\.0</HexalithEventStoreVersion>', catalog.read_text(encoding='utf-8-sig'))
assert '17d0c6ffec5fb3c8d8058e67cb85644460cb5256' in subprocess.check_output(['git', 'ls-tree', baseline, '--', 'references/Hexalith.Builds'], text=True)

assert len(re.findall(r'<PackageVersion Include="Hexalith\.EventStore\.[^"]+" Version="\$\(HexalithEventStoreVersion\)"', catalog.read_text(encoding='utf-8-sig'))) == 13
assert '9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b' in subprocess.check_output(['git', 'ls-tree', 'b3878f1488d2a4a097bac2e5c233fa07da6eb2cf', '--', 'references/Hexalith.Builds'], text=True)
historical_catalog = subprocess.check_output(['git', '-C', 'references/Hexalith.Builds', 'show', '9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b:Props/Directory.Packages.props'], text=True)
assert re.search(r'<HexalithEventStoreVersion\b[^>]*>3\.120\.0</HexalithEventStoreVersion>', historical_catalog)

assert 'Those results do not qualify the current `3.126.0` catalog.' in context
panel = Path('src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor').read_text()
snapshot = Path('src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs').read_text()
assert 'next = next.WithCorrectiveProof(null);' in panel
assert 'return this with { AuditState = TenantCommandAuditState.MissingSupport, ProofLink = null };' in snapshot
old_story = (artifacts / '5-6-preview-and-confirm-correction-with-linked-proof.md').read_text()
assert 'Superseded behavior (2026-10-21; DW-504)' in old_story and 'renders no paired proof link' in old_story
clarification = (artifacts / 'spec-eventstore-3-117-1.md').read_text()
assert '**structural equality of parsed rows**' in clarification
assert 'preservation of every catalog byte except the intended version substitution' in clarification
verifier = Path('src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs').read_text()
assert 'model.Administrators.Contains(claim.UserId)' in verifier
auth = Path('docs/production-auth-readiness.md').read_text()
for phrase in ['GlobalAdministratorRemoved', 'token or circuit renewal', 'There is no bounded revocation guarantee', 'DW-562 and DW-617']:
    assert phrase in auth
assert 'Those stale claims do not override the persisted-membership verifier' in auth
assert 'An operation also permitted to a tenant owner does not prove global authority was revoked' in auth
# New relative Markdown link destinations resolve; ignore original historical links.
paths = [artifacts / n for n in endpoints] + [Path('_bmad-output/project-context.md'), Path('docs/production-auth-readiness.md'), ledger, index]
for path in paths:
    old_links = set(re.findall(r'\]\(([^)]+)\)', original(path)))
    for link in set(re.findall(r'\]\(([^)]+)\)', path.read_text())) - old_links:
        assert (path.parent / link.split('#', 1)[0]).is_file(), (str(path), link)
assert not subprocess.check_output(['git', 'diff', baseline, '--', 'src', 'tests', 'references', '.github', 'Directory.Build.props', 'Directory.Packages.props', 'global.json'])
result = dict(baseline=baseline, selected=sorted(selected), result='pass', total_records=657, open_records=212, done_records=445, index_rows=193, dispositions=dict(counts), historical_audit_endpoints=endpoints, changed_document_sha256={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths})
parser = argparse.ArgumentParser()
parser.add_argument('--write-result', action='store_true', help='Explicitly regenerate evidence after an intentional correction.')
parser.add_argument('--result-path', type=Path, default=artifacts / 'dw-504-532-564-578-596-ledger-validation.json')
args = parser.parse_args()
if args.write_result:
    args.result_path.write_text(json.dumps(result, indent=2) + '\n')
else:
    assert json.loads(args.result_path.read_text()) == result, 'saved evidence differs; inspect before explicit regeneration'
print('PASS: exactly five closures; 657 records / 212 open / 193 rows; history, unrelated work, links, source facts and historical audit policy verified.')
```


## Commit validation and synchronization

The installed and lockfile-pinned commitlint CLI are both `21.2.3`. The exact full candidate, including its final newline, is saved in `/tmp/tenants-doc-reconcile-commit-message.txt`:

```text
docs: reconcile five deferred evidence and guidance records
```

`node_modules/.bin/commitlint --verbose < /tmp/tenants-doc-reconcile-commit-message.txt` passes with zero problems and zero warnings. `git fetch origin main` succeeds and the initial `git rev-list --left-right --count HEAD...origin/main` reports `0 0`. Independent review and final checks precede commit/push; the committed-event guard is checked after commit and before push. The user explicitly authorized pushing main.


## Independent review corrections

Blind Hunter reviewed 39.490 kB with finding floor 7. All seven findings are triaged in the owning spec and patched within this documentation scope: qualify the catalog substitution exception; distinguish stale claims from effective server authority; add observable revocation checklist criteria; supply current ledger verification paths and preserve original locations; reproduce the excluded paging history; compare saved evidence by default; distinguish story-validator output from silent whitespace success. No finding is deferred. Final recheck follows.

The paging investigation is reproduced by the embedded script using these read-only commands:

```bash
git merge-base --is-ancestor 23b2a7691ae667fd160a48a5d7293f3cf38d48fe 092bcd0eafffb9e85b6c3506ee532f1340d6e87f
git merge-base --is-ancestor 6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513 23b2a7691ae667fd160a48a5d7293f3cf38d48fe
git merge-base --is-ancestor 6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513 092bcd0eafffb9e85b6c3506ee532f1340d6e87f
git log --follow --format=%H 092bcd0eafffb9e85b6c3506ee532f1340d6e87f -- _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md
```

The ancestry exits are 1, 0 and 0 respectively. `git show <revision>:<paging-path>` for each of the four history entries yields: baseline_commit absent in `7716f0b11423eb54d74935b6cc6e3edf405dc400`, and `23b2a7691ae667fd160a48a5d7293f3cf38d48fe` in `5a3bc6dd4c1aa77cec50d0a960e986aa89c08cbe`, `10c9f6f66632f6861fe13fb9f41ab05b90c37f67` and `ded413dfd4786983585c7f56a399825cc1afbae3`. All four retain baseline_revision `6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`.

Post-review audit: explicit `--write-result` regenerated the corrected six-document hashes, then default checking passed. A separate `/tmp/tenants-doc-reconcile-mismatch.json` with `done_records=-1` fails with exit 1 and the expected saved-evidence-mismatch diagnostic. Both the committed JSON and control JSON remain byte-identical before/after the failed check, and the subsequent successful default check also preserves saved bytes. The embedded and executed script contents match exactly.

Final independent recheck: all seven findings resolved; no remaining concrete finding. The reviewer re-ran the default checker, confirmed identical embedded/executed code, unchanged saved JSON bytes and all six document hashes. Final entry-to-origin comparison remains 0/0, and the exact unchanged full commit candidate passes pinned commitlint 21.2.3 with zero problems/warnings. Candidate SHA-256: `3fb748b31ac118c2d9e20623837bc04b8ec5ec228a7ddcac5204829aa126a5a6`. Only the post-commit event guard and push remain operational steps.
