"""Audit the direct documented seven-category IR contract before the separate workflow edit."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

bindings = json.loads((BASE / 'source-bindings.json').read_text())
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER).decode().strip() == bindings['source_base_commit']
assert subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode() == bindings['owner_status_before']
assert not subprocess.check_output(['git', 'diff', '--name-only'], cwd=OWNER).strip()
for path, row in bindings['read_inputs'].items():
    assert sha(OWNER / path) == row['sha256']
for path, expected in bindings['working_input_overlay'].items():
    assert sha(OWNER / path) == expected
receipt = json.loads((BASE / 'attempt-01/verification.json').read_text())
for name, expected in receipt['private_test_sha256'].items():
    assert sha(BASE / name) == expected
for command in receipt['commands']:
    assert sha(BASE / 'attempt-01' / command['log']) == command['sha256']
    assert command['exit_code'] == (2 if command['name'].endswith('-tests') and not command['name'].startswith('baseline') else 0)
results = []
for trx in sorted((BASE / 'attempt-01').rglob('tests.trx')):
    ident = trx.parent.name.removesuffix('-results')
    rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
    assert len(rows) == 7
    failed = [row['testName'] for row in rows if row['outcome'] == 'Failed']
    assert len(failed) == (0 if ident == 'baseline' else 3 if ident == '14143' else 1)
    results.append({'control': ident, 'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx), 'outcomes': dict(Counter(row['outcome'] for row in rows)), 'failed_tests': failed})
assert len(results) == 6
package = json.loads((BASE / 'package-check-02/verification.json').read_text())
for command in package['commands']:
    assert command['exit_code'] == 0 and sha(BASE / 'package-check-02' / command['output']) == command['sha256']
assert package['patch_sha256'] == sha(BASE / 'private-column-filter-proposal.patch')
assert sum(row['cases'] for row in json.loads((BASE / 'package-check-02/style-check.json').read_text())) == 7
jobs = [str(Path(receipt['workspace']).parent), str(Path(package['workspace']).parent)]
assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
summary = {'scope': __doc__, 'source_base_commit': bindings['source_base_commit'], 'working_input_overlay': bindings['working_input_overlay'],
           'public_contract_only': True, 'generated_ui_effect_claimed': False, 'baseline_passed': 7, 'literal_controls': 5, 'literal_detections': 5,
           'results': results, 'patch_sha256': package['patch_sha256'], 'owner_unchanged_at_phase1_completion': True,
           'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []}, 'accepted_triage_authored': False, 'qualification_claim': False}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)} for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'files': manifest}, indent=2) + '\n')
print(json.dumps({'baseline': 7, 'literal_detections': 5, 'jobs_cleaned': 2, 'owner_unchanged': True}))
