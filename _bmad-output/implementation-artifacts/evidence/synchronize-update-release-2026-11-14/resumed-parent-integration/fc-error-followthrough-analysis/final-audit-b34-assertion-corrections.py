"""Audit final b34 working-input focused tests, preserved previous bytes and owner-change bounds."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
PRIOR = BASE.parent / 'fc-error-followthrough-analysis-prior-b34-assertions-01'
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'
TEST_DIRECTORY = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'
SOURCE = 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
E0 = 'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=OWNER).decode().strip()

preserved = json.loads((PRIOR / 'verbatim-prior-packet.json').read_text())
mutable = {'README.md', 'summary.json', 'evidence-manifest.json', 'private-test-proposal.patch'}
for row in preserved['files']:
    assert sha(PRIOR / row['path']) == row['sha256']
    if row['path'] not in mutable and not row['path'].startswith('private-test-proposal/'):
        assert sha(BASE / row['path']) == row['sha256'], row['path']

old_inputs = PRIOR / 'owner-test-inputs'
changed = []
unchanged = []
assert {path.name for path in old_inputs.glob('*.cs')} == {path.name for path in (OWNER / TEST_DIRECTORY).glob('*.cs')}
for path in sorted(old_inputs.glob('*.cs')):
    current = OWNER / TEST_DIRECTORY / path.name
    row = {'path': TEST_DIRECTORY + '/' + path.name, 'before_sha256': sha(path), 'after_sha256': sha(current)}
    (changed if path.read_bytes() != current.read_bytes() else unchanged).append(row)
assert {Path(row['path']).name for row in changed} == {'ErrorFollowthroughBehaviorTests.cs', 'ProjectionParserSurvivorRegressionTests.cs'}
assert git('rev-parse', 'HEAD') == SOURCE
assert not git('diff', '--name-only')

attempts = []
jobs = list(json.loads((PRIOR / 'summary.json').read_text())['cleanup']['owned_job_paths_removed'])
for number in ('08', '09', '10'):
    path = BASE / ('b34-assertion-corrections-' + number)
    receipt = json.loads((path / 'verification.json').read_text())
    assert receipt['source_base_commit'] == SOURCE and receipt['builds_commit'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
    jobs.append(str(Path(receipt['workspace']).parent))
    for command in receipt['commands']:
        assert sha(path / command['log']) == command['sha256']
        assert command['exit_code'] == (2 if number == '08' and command['name'] == 'focused-tests' else 0)
    trx = path / 'results/tests.trx'
    rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
    assert len(rows) == 38 and all(row['outcome'] in ('Passed', 'Failed') for row in rows)
    outcomes = dict(Counter(row['outcome'] for row in rows))
    assert outcomes == ({'Passed': 36, 'Failed': 2} if number == '08' else {'Passed': 38})
    inputs = path / 'test-inputs' if number != '10' else BASE / 'private-test-proposal'
    assert receipt['test_overlay_sha256'] == {TEST_DIRECTORY + '/' + file.name: sha(file) for file in inputs.glob('*.cs')}
    if number == '10':
        assert receipt['patch_sha256'] == sha(BASE / 'private-test-proposal.patch')
        assert all((OWNER / name).read_bytes() == (inputs / Path(name).name).read_bytes() for name in receipt['test_overlay_sha256'])
        current_receipt = receipt
    else:
        assert receipt['patch_sha256'] == sha(path / 'prospective.patch')
    attempts.append({'path': str(path.relative_to(BASE)), 'receipt_sha256': sha(path / 'verification.json'),
                     'trx': str(trx.relative_to(BASE)), 'trx_sha256': sha(trx), 'outcomes': outcomes,
                     'failed_tests': [row['testName'] for row in rows if row['outcome'] == 'Failed']})

jobs = sorted(set(jobs))
assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
trees = {path: {'original_e0': git('rev-parse', E0 + ':' + path), 'current_b34': git('rev-parse', SOURCE + ':' + path)}
         for path in ('src', 'tests', 'eng', '.github/workflows')}
assert all(row['original_e0'] == row['current_b34'] for row in trees.values())
summary = {'scope': __doc__, 'source_base_commit': SOURCE, 'test_overlay_sha256': current_receipt['test_overlay_sha256'],
           'source_attribution': 'Current b34 immutable tracked archive plus the four exact working-input test files; a focused test receipt, not a clean committed source or mutation qualification.',
           'source_tree_identity': trees, 'owner_tracked_changes': [], 'owner_test_changes': changed,
           'other_owner_test_files_unchanged': unchanged, 'owner_status_observed': git('status', '--porcelain=v1'),
           'prior_packet': str(PRIOR.relative_to(BASE.parent)), 'prior_packet_files_verified': len(preserved['files']),
           'prior_packet_manifest_sha256': sha(PRIOR / 'verbatim-prior-packet.json'),
           'original_error_analysis_source': E0, 'original_error_ledger_sha256': sha(BASE / 'per-id-ledger.json'),
           'current_prospective_patch_sha256': sha(BASE / 'private-test-proposal.patch'), 'proposal_total_cases': 39,
           'executed_cases_in_two_edited_classes': 38, 'focused_attempts': attempts,
           'preparation_refusal': {'path': 'b34-assertion-corrections-07/preparation-refusal.json', 'build_or_tests_executed': False,
                                  'cleanup_bound': 'run-with-temp exited and cleaned before any compiler/test child launch; failed preparation did not record its random scratch path.'},
           'reviewer_premise_correction': 'reviewer-premise-correction-b34.json',
           'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []},
           'full_mutation_run': False, 'accepted_triage_authored': False, 'qualification_claim': False,
           'remaining': 'Parent owns broader verification and any full current-source qualification/publication.'}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)}
            for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source_base_commit': SOURCE, 'test_overlay_sha256': current_receipt['test_overlay_sha256'], 'files': manifest}, indent=2) + '\n')
print(json.dumps({'focused_cases_passed': 38, 'owner_files_changed': len(changed), 'other_owner_fixtures_preserved': len(unchanged),
                  'prior_packet_files_verified': len(preserved['files']), 'owned_jobs_removed': len(jobs), 'full_mutation_run': False}))
