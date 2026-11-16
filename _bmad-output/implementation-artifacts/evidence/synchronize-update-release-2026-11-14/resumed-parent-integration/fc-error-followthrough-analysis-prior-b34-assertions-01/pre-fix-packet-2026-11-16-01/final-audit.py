"""Audit preserved focused receipts and cleanup; no new build, test or mutation run."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
SOURCE = 'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
REPORT = BASE.parent / 'fc-error-local-diagnostic/attempt-01/original-error-artifacts/reports/source-tools-error-handling.json'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=OWNER).decode().strip()

ledger = json.loads((BASE / 'per-id-ledger.json').read_text())
report = json.loads(REPORT.read_text())
observed = {m['id']: (path, data, m) for path, data in report['files'].items()
            for m in data.get('mutants', []) if m['status'] in ('Survived', 'NoCoverage', 'Timeout')}
assert len(observed) == len(ledger['mutants']) == 817
assert ledger['report_sha256'] == sha(REPORT) == '44079e6dd90db8bf8370b59dd90e0049fb12a9ef5643f403e738ab15172e23bc'
for row in ledger['mutants']:
    path, data, original = observed[row['id']]
    assert row['observed_status'] == original['status'] and row['location'] == original['location'] and row['replacement'] == original['replacement']
    assert row['source_sha256'] == hashlib.sha256(data['source'].encode()).hexdigest()
    assert row['owner'] is None and row['accepted_action'] is None and row['owner_approval'] is False
    for control in row['private_controls']:
        assert sha(BASE.parent / control['trx']) == control['sha256']

attempts = []
workspaces = []
for relative, expected in [('behavior-controls-01', 25), ('behavior-controls-02', 25), ('proposal-controls-03', 39)]:
    directory = BASE / relative
    receipt = json.loads((directory / 'verification.json').read_text())
    assert receipt['source'] == SOURCE and receipt['builds'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
    workspaces.append(receipt['workspace'])
    for command in receipt['commands']:
        assert sha(directory / command['log']) == command['sha256']
    tests = []
    for trx in sorted(directory.rglob('*.trx')):
        rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
        assert len(rows) == expected
        outcomes = dict(Counter(row['outcome'] for row in rows))
        assert outcomes.get('NotExecuted', 0) == 0
        baseline = trx.parent.name == 'baseline-results'
        assert (outcomes == {'Passed': expected}) if baseline else outcomes.get('Failed', 0) > 0
        tests.append({'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx), 'outcomes': outcomes,
                      'failed_tests': [row['testName'] for row in rows if row['outcome'] == 'Failed']})
    for ident, variant in receipt['variants'].items():
        _, data, original = observed[ident]
        source = data['source']
        start, end = original['location']['start'], original['location']['end']
        lines = source.splitlines(keepends=True)
        a = sum(map(len, lines[:start['line'] - 1])) + start['column'] - 1
        b = sum(map(len, lines[:end['line'] - 1])) + end['column'] - 1
        changed = source[:a] + original['replacement'] + source[b:]
        assert variant['baseline_source_sha256'] == hashlib.sha256(source.encode()).hexdigest()
        assert variant['mutated_source_sha256'] == hashlib.sha256(changed.encode()).hexdigest()
        assert variant['baseline_source_sha256'] != variant['mutated_source_sha256']
    attempts.append({'attempt': relative, 'commands': len(receipt['commands']), 'nonzero_commands': [
        {'name': command['name'], 'exit_code': command['exit_code']} for command in receipt['commands'] if command['exit_code']], 'tests': tests})

for relative in ['scan-verification.json', 'scan-attempt-01/scan-verification.json', 'package-check-04/verification.json']:
    path = BASE / relative
    receipt = json.loads(path.read_text())
    workspaces.append(receipt['workspace'])
    for command in receipt['commands']:
        assert sha(path.parent / command['output']) == command['sha256']
tokens = json.loads((BASE / 'package-check-04/token-equality.json').read_text())
assert len(tokens) == 2 and all(row['equal'] for row in tokens)
package = json.loads((BASE / 'package-check-04/verification.json').read_text())
assert package['patch_sha256'] == sha(BASE / 'private-test-proposal.patch')
assert all(command['exit_code'] == 0 for command in package['commands'])

jobs = sorted({str(Path(work).parents[0]) for work in workspaces})
assert all(path.startswith('/dev/shm/hexalith-job.') for path in jobs)
assert all(not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
owned = [line.strip() for line in processes if any(path in line for path in jobs)]
assert not owned
owner_status = git('status', '--porcelain=v1')
assert owner_status == ''
owner_head = git('rev-parse', 'HEAD')
assert owner_head == '695d23149c56a6104e81d23a54116b4fb914df95'
trees = {path: {'diagnostic_e0': git('rev-parse', SOURCE + ':' + path),
                'current_owner': git('rev-parse', 'HEAD:' + path)}
         for path in ('src', 'tests', 'eng', '.github/workflows', '.config')}
assert all(row['diagnostic_e0'] == row['current_owner'] for row in trees.values())

summary = {'scope': __doc__, 'source': SOURCE, 'original_report_sha256': sha(REPORT),
           'owner_head_observed': owner_head, 'owner_status': owner_status,
           'diagnostic_input_tree_identity': trees, 'qualification_claim': False,
           'observed_status_counts': dict(Counter(row['observed_status'] for row in ledger['mutants'])),
           'proposed_disposition_counts': dict(Counter(row['proposed_disposition'] for row in ledger['mutants'])),
           'cause_groups': len(json.loads((BASE / 'cause-groups.json').read_text())['groups']),
           'private_proposal_cases': 39, 'new_compiled_literal_variants_detected': 27,
           'earlier_private_parser_variants_detected': 15,
           'strict_compile_refusals_preserved': ['14124 CS8794 in behavior-controls-01; no runtime claim', '12207 CS8602 in earlier packet; separate warning-visible runtime-only control'],
           'focused_attempts': attempts,
           'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': owned},
           'accepted_triage_authored': False, 'full_error_rerun': False,
           'remaining': ['Owner independently decides each bounded equivalence/deferred/blocking candidate.',
                         'Hash collision/performance, implicit generic and public external consumers remain unresolved.',
                         'Current-source complete dual-segment qualification and genuine owner/follow-up assignment remain parent-owned.']}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)}
            for path in sorted(BASE.rglob('*')) if path.is_file() and path.name != 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source': SOURCE, 'files': manifest}, indent=2) + '\n')
print(json.dumps({'ids': len(observed), 'groups': summary['cause_groups'], 'proposal_cases': 39,
                  'new_literal_detections': 27, 'owned_jobs_removed': len(jobs), 'owner_clean': True, 'manifest_files': len(manifest)}))
