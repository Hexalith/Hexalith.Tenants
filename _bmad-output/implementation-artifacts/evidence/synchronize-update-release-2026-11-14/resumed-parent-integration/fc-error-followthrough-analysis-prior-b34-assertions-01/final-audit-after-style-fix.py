"""Verify the smallest private-package style fix, fresh focused receipts and preserved historical bytes."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
PRIOR = BASE / 'pre-fix-packet-2026-11-16-01'
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

preserved = json.loads((PRIOR / 'preserved-bytes.json').read_text())
for row in preserved['files']:
    assert sha(PRIOR / row['path']) == row['sha256']
old_manifest = json.loads((PRIOR / 'evidence-manifest.json').read_text())
for row in old_manifest['files']:
    path = PRIOR / row['path'] if (PRIOR / row['path']).is_file() else BASE / row['path']
    assert sha(path) == row['sha256'], row['path']

attempt = BASE / 'proposal-controls-05'
receipt = json.loads((attempt / 'verification.json').read_text())
assert receipt['source'] == 'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
assert receipt['builds'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
assert receipt['test_source_sha256'] == {file.name: sha(file) for file in (BASE / 'private-test-proposal').glob('*.cs')}
assert len(receipt['test_source_sha256']) == 4
commands = {row['name']: row for row in receipt['commands']}
assert len(commands) == 6
for name, row in commands.items():
    assert sha(attempt / row['log']) == row['sha256']
    assert row['exit_code'] == (2 if name in ('13689-tests', '13690-tests') else 0)
tests = []
for trx in sorted(attempt.rglob('*.trx')):
    rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
    outcomes = dict(Counter(row['outcome'] for row in rows))
    assert len(rows) == 39
    failed = [row['testName'] for row in rows if row['outcome'] == 'Failed']
    if trx.parent.name == 'baseline-results':
        assert outcomes == {'Passed': 39}
    else:
        assert outcomes == {'Passed': 38, 'Failed': 1} or outcomes == {'Failed': 1, 'Passed': 38}
        assert failed == ['Hexalith.FrontComposer.SourceTools.Tests.Mutation.TemplateParserProviderBoundaryTests.OtherAttributeProviderReturnsAnEmptyTemplateResult']
    tests.append({'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx), 'outcomes': outcomes, 'failed_tests': failed})
assert len(tests) == 3

package = BASE / 'package-check-06'
package_receipt = json.loads((package / 'verification.json').read_text())
for command in package_receipt['commands']:
    assert command['exit_code'] == 0 and sha(package / command['output']) == command['sha256']
assert package_receipt['patch_sha256'] == sha(BASE / 'private-test-proposal.patch')
checks = json.loads((package / 'style-token-check.json').read_text())
assert all(check['passed'] for check in checks)

summary = json.loads((PRIOR / 'summary.json').read_text())
jobs = sorted(set(summary['cleanup']['owned_job_paths_removed'] + [str(Path(receipt['workspace']).parent), str(Path(package_receipt['workspace']).parent)]))
assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
owner_head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER).decode().strip()
owner_status = subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode().strip()
assert owner_head == summary['owner_head_observed'] and owner_status == ''
summary.update({'scope': __doc__, 'pre_fix_packet': str(PRIOR.relative_to(BASE)),
                'pre_fix_preserved_files': len(preserved['files']), 'historical_manifest_files_verified': len(old_manifest['files']),
                'current_private_proposal_files': receipt['test_source_sha256'], 'current_patch_sha256': sha(BASE / 'private-test-proposal.patch'),
                'current_focused_attempt': {'path': str(attempt.relative_to(BASE)), 'tests': tests, 'rerun_literal_ids': ['13689', '13690']},
                'current_package_checks': {'path': str(package.relative_to(BASE)), 'checks': len(checks), 'all_passed': True},
                'owner_status': owner_status, 'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []}})
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)}
            for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source': receipt['source'], 'files': manifest}, indent=2) + '\n')
print(json.dumps({'cases_passed': 39, 'literal_controls_preserved': 2, 'proposal_files': 4,
                  'historical_files_verified': len(old_manifest['files']), 'owned_jobs_removed': len(jobs), 'owner_clean': True}))
