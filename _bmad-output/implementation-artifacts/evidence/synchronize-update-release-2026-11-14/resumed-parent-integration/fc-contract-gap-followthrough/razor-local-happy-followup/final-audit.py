"""Audit the local-report-bound private Razor recovery proposal and unchanged owner/preceding packet."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
OWNER = BASE.parents[6] / 'references/Hexalith.FrontComposer'
REPORT = BASE.parents[1] / 'fc-happy-local-diagnostic/attempt-03/post-validator-artifacts/happy-path/reports/source-tools-happy-path.json'
bindings = json.loads((BASE / 'source-bindings.json').read_text())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=OWNER).decode().strip()

assert sha(REPORT) == '91144b286ea92cf254d0e4039384f99d2eeaaf84aac4fb1dcd7c0b41743abb71'
report = json.loads(REPORT.read_text())
counts = Counter(mutant['status'] for data in report['files'].values() for mutant in data.get('mutants', []))
assert {key: counts[key] for key in ('Killed', 'Survived', 'Timeout', 'NoCoverage')} == {'Killed': 1783, 'Survived': 549, 'Timeout': 1, 'NoCoverage': 131}
assert git('rev-parse', 'HEAD') == bindings['source_base_commit'] == 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
assert subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode() == bindings['owner_status_before']
assert not git('diff', '--name-only')
for path, row in bindings['read_inputs'].items():
    assert sha(OWNER / path) == row['sha256'] and git('rev-parse', 'HEAD:' + path) == row['git_blob']
for path, expected in bindings['working_input_overlay'].items():
    assert sha(OWNER / path) == expected
additional = ['src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs', 'src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs']
(BASE / 'canonical-severity-bindings.json').write_text(json.dumps({path: {'sha256': sha(OWNER / path), 'git_blob': git('rev-parse', 'HEAD:' + path)} for path in additional}, indent=2) + '\n')

preceding_manifest = json.loads((BASE.parent / 'evidence-manifest.json').read_text())
for row in preceding_manifest['files']:
    assert sha(BASE.parent / row['path']) == row['sha256'], row['path']

inventory = json.loads((BASE / 'complete-local-problems-and-nocoverage.json').read_text())
mapped = {row['id']: row for row in inventory['rows']}
index = json.loads((BASE.parents[1] / 'fc-error-followthrough-analysis/source-index.json').read_text())
nocoverage = []
for row in inventory['rows']:
    if row['local_status'] != 'NoCoverage':
        continue
    candidates = [member for member in index['members'] if member['path'] == row['path'] and member['startLine'] <= row['location']['start']['line'] <= member['endLine']]
    member = min(candidates, key=lambda member: member['endLine'] - member['startLine'])['name'] if candidates else None
    nocoverage.append({**row, 'member': member, 'hash_member': bool(member and member.endswith('.GetHashCode'))})
assert len(nocoverage) == 131 and sum(row['hash_member'] for row in nocoverage) == 25
(BASE / 'all-local-nocoverage-spans.json').write_text(json.dumps({'report_sha256': sha(REPORT), 'hash_rows': 25, 'non_hash_rows': 106, 'rows': nocoverage}, indent=2) + '\n')

receipt = json.loads((BASE / 'attempt-01/verification.json').read_text())
assert receipt['source_base_commit'] == bindings['source_base_commit'] and receipt['builds_commit'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
for name, expected in receipt['private_test_sha256'].items():
    assert sha(BASE / name) == expected
for command in receipt['commands']:
    assert sha(BASE / 'attempt-01' / command['log']) == command['sha256']
    assert command['exit_code'] in (0, 2)
    if command['name'].endswith('-build'):
        assert command['exit_code'] == 0
controls = []
tests = []
for trx in sorted((BASE / 'attempt-01').rglob('tests.trx')):
    ident = trx.parent.name.removesuffix('-results')
    rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
    assert len(rows) == 5 and all(row['outcome'] in ('Passed', 'Failed') for row in rows)
    failed = [row['testName'] for row in rows if row['outcome'] == 'Failed']
    tests.append({'control': ident, 'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx), 'outcomes': dict(Counter(row['outcome'] for row in rows)), 'failed_tests': failed})
    if ident == 'baseline':
        assert not failed
        continue
    variant = receipt['variants'][ident]
    assert sha(BASE / 'attempt-01' / ident / variant['path']) == variant['literal_source_sha256']
    assert variant['original_status'] == mapped[ident]['local_status']
    assert variant['original'] == mapped[ident]['original'] and variant['replacement'] == mapped[ident]['replacement']
    assert bool(failed) == (ident not in ('14940', '14950'))
    controls.append({'id': ident, 'local_original_status': mapped[ident]['local_status'], 'source': mapped[ident], 'actual_control': tests[-1],
                     'baseline_source_sha256': variant['baseline_source_sha256'], 'literal_source_sha256': variant['literal_source_sha256'],
                     'private_test_sha256': receipt['private_test_sha256'], 'owner': None, 'accepted_action': None})
assert len(controls) == 14 and sum(bool(row['actual_control']['failed_tests']) for row in controls) == 12
(BASE / 'literal-controls.json').write_text(json.dumps({'scope': 'Complete-local-report original statuses plus actual private controls; never hosted status transfer or accepted triage.', 'controls': controls}, indent=2) + '\n')

package = json.loads((BASE / 'package-check-02/verification.json').read_text())
for command in package['commands']:
    assert command['exit_code'] == 0 and sha(BASE / 'package-check-02' / command['output']) == command['sha256']
assert package['patch_sha256'] == sha(BASE / 'private-razor-recovery-proposal.patch')
assert sum(row['cases'] for row in json.loads((BASE / 'package-check-02/style-check.json').read_text())) == 5
jobs = [str(Path(receipt['workspace']).parent), str(Path(package['workspace']).parent)]
assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
summary = {'scope': __doc__, 'source_base_commit': bindings['source_base_commit'], 'working_input_overlay': bindings['working_input_overlay'],
           'local_original_report_sha256': sha(REPORT), 'local_original_status_counts': dict(counts), 'local_hosted_boundary': {'id': '12812', 'local': 'Killed', 'hosted_parent_console_observation': 'rawSuccess', 'transfer_permitted': False},
           'local_nocoverage': {'hash': 25, 'non_hash': 106, 'total': 131}, 'proposal_cases': 5, 'baseline_passed': 5,
           'literal_controls': 14, 'literal_detections': 12, 'literal_survivors': {'14940': 'List capacity only in this output contract; allocation/extreme inputs remain bounded unknowns.', '14950': 'Canonical HFC1025 descriptor controls emitted severity; raw transform diagnostic consumers remain a boundary.'},
           'uncovered_extra_id': {'14143': 'Unused documented public getter has no tracked production reader; no actual UI effect asserted.'},
           'tests': tests, 'patch_sha256': package['patch_sha256'], 'preceding_packet_files_verified_unchanged': len(preceding_manifest['files']),
           'owner_unchanged': True, 'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []},
           'full_mutation_run': False, 'accepted_triage_authored': False, 'gate_or_tool_changes': False, 'qualification_claim': False,
           'remaining': 'Original threshold failure remains unresolved; parent owns any source integration, full current-source qualification and disposition.'}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)} for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source_base_commit': bindings['source_base_commit'], 'local_report_sha256': sha(REPORT), 'files': manifest}, indent=2) + '\n')
print(json.dumps({'baseline_passed': 5, 'literal_detections': 12, 'literal_survivors': 2, 'prior_files_unchanged': len(preceding_manifest['files']), 'jobs_cleaned': 2, 'owner_unchanged': True}))
