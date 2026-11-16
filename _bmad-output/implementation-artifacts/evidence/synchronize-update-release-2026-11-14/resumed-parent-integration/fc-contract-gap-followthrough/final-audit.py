"""Bind the private final recovery proposal to exact focused receipts; no gate or owner acceptance."""
from pathlib import Path
from collections import Counter
import hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'
PRIOR = BASE.parent / 'fc-contract-gap-followthrough-pre-recovery-01'
bindings = json.loads((BASE / 'source-bindings.json').read_text())
original_ledger_path = BASE.parent / 'fc-error-followthrough-analysis/per-id-ledger.json'
original_ledger = json.loads(original_ledger_path.read_text())
ledger = {row['id']: row for row in original_ledger['mutants']}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=OWNER).decode().strip()

assert git('rev-parse', 'HEAD') == bindings['source_base_commit'] == 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
assert subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode() == bindings['owner_status_before']
assert not git('diff', '--name-only')
for path, row in bindings['read_inputs'].items():
    assert sha(OWNER / path) == row['sha256'] and git('rev-parse', 'HEAD:' + path) == row['git_blob']
for path, expected in bindings['working_input_overlay'].items():
    assert sha(OWNER / path) == expected
additional_paths = ['src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs', 'tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs',
                    'src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs', 'src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs',
                    'tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs']
(BASE / 'additional-caller-bindings.json').write_text(json.dumps({path: {'sha256': sha(OWNER / path), 'git_blob': git('rev-parse', 'HEAD:' + path)} for path in additional_paths}, indent=2) + '\n')

prior_manifest = []
mutable = {path.name for path in BASE.glob('*Tests.cs')} | {'private-contract-gap-proposal.patch'}
for path in sorted(PRIOR.rglob('*')):
    if not path.is_file():
        continue
    relative = str(path.relative_to(PRIOR))
    if relative == 'pre-recovery-manifest.json':
        continue
    prior_manifest.append({'path': relative, 'bytes': path.stat().st_size, 'sha256': sha(path)})
    if relative not in mutable:
        assert sha(BASE / relative) == sha(path), relative
(PRIOR / 'pre-recovery-manifest.json').write_text(json.dumps({'scope': 'Verbatim packet preserved before recovery expansion; all unchanged raw/root files verified.', 'files': prior_manifest}, indent=2) + '\n')

happy = {}
for name in ('partial-happy-matching-observations.json', 'partial-happy-followup-observations.json'):
    for row in json.loads((BASE / name).read_text())['observed_matching_ids']:
        happy.setdefault(row['id'], []).append({'snapshot': name, **row})
controls = {}
runs = []
jobs = []
attempts = []
for number in ('01', '02', '03', '05', '06'):
    directory = BASE / ('attempt-' + number)
    receipt = json.loads((directory / 'verification.json').read_text())
    assert receipt['source_base_commit'] == bindings['source_base_commit'] and receipt['builds_commit'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
    jobs.append(str(Path(receipt['workspace']).parent))
    private_inputs = directory / 'private-inputs' if number in ('01', '02', '05') else (PRIOR if number == '03' else BASE)
    for name, expected in receipt['private_test_sha256'].items():
        assert sha(private_inputs / name) == expected
    for command in receipt['commands']:
        assert sha(directory / command['log']) == command['sha256']
    command_map = {command['name']: command for command in receipt['commands']}
    for trx in sorted(directory.rglob('tests.trx')):
        values = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
        assert all(row['outcome'] in ('Passed', 'Failed') for row in values)
        ident = trx.parent.name.removesuffix('-results')
        result = {'attempt': number, 'control': ident, 'count': len(values), 'outcomes': dict(Counter(row['outcome'] for row in values)),
                  'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx),
                  'failed_tests': [row['testName'] for row in values if row['outcome'] == 'Failed']}
        runs.append(result)
        if ident == 'baseline':
            if number in ('02', '03', '06'):
                assert not result['failed_tests']
            if number == '06':
                assert len(values) == 25
            continue
        variant = receipt['variants'][ident]
        source = directory / ident / variant['path']
        assert sha(source) == variant['literal_source_sha256']
        original = ledger[ident]
        assert variant['original_status'] == original['observed_status'] and variant['original_location'] == original['location']
        assert variant['original'] == original['original'] and variant['replacement'] == original['replacement']
        if ident not in controls:
            controls[ident] = {'id': ident, 'original_error_source': original_ledger['source'], 'original_error_status': original['observed_status'],
                               'path': original['path'], 'member': original['member'], 'location': original['location'], 'original': original['original'], 'replacement': original['replacement'],
                               'matching_raw_happy_observations': happy.get(ident, []), 'receipts': [], 'owner': None, 'accepted_action': None}
        controls[ident]['receipts'].append({**result, 'baseline_source_sha256': variant['baseline_source_sha256'], 'literal_source_sha256': variant['literal_source_sha256'],
                                           'private_test_sha256': receipt['private_test_sha256'], 'test_exit': command_map[ident + '-tests']['exit_code']})
    attempts.append({'attempt': number, 'receipt_sha256': sha(directory / 'verification.json'), 'commands': len(receipt['commands']),
                     'nonzero_exits': [{'name': row['name'], 'exit_code': row['exit_code']} for row in receipt['commands'] if row['exit_code']]})

for ident, row in controls.items():
    current = [receipt for receipt in row['receipts'] if receipt['attempt'] == '06']
    row['final_recovery_proposal_literal_detected'] = bool(current and current[-1]['failed_tests'])
    row['final_recovery_proposal_literal_run'] = bool(current)
    row['any_earlier_private_literal_detected'] = any(receipt['failed_tests'] for receipt in row['receipts'] if receipt['attempt'] != '06')
(BASE / 'per-control-findings.json').write_text(json.dumps({'scope': 'Actual finite controls only; source-specific observations, never accepted triage or a mutation score.', 'controls': list(controls.values())}, indent=2) + '\n')

limits = {
    '12935': 'Current sole caller short-circuits null/whitespace before the private trimmed-empty guard. Negative private control survives; no internal/reflection-only fixture is invented.',
    '13050': 'A null TypedConstant.Type after earlier IsNull handling needs a supported source/metadata fixture. Reachability remains unresolved.',
    '13912': 'Internal formatter null-type guard has no demonstrated ordinary Parse/generated-output caller; direct internal null invocation is not treated as a product defect.',
    '13915': 'Internal formatter null-compilation guard has no demonstrated ordinary Parse/generated-output caller.',
    '14003': 'Both selected aliases are admitted and generate compiled assignments here. Exact-string/public-client stability remains bounded; no accepted equivalence.',
    '14025': 'Function-pointer alias collection needs a real supported consumer contract; unsafe assignment is not newly admitted.'}
remaining = []
for row in original_ledger['mutants']:
    if row['observed_status'] != 'NoCoverage':
        continue
    control = controls.get(row['id'])
    remaining.append({'id': row['id'], 'path': row['path'], 'member': row['member'], 'location': row['location'], 'original': row['original'], 'replacement': row['replacement'],
                      'original_error_status': 'NoCoverage', 'private_control_present': control is not None,
                      'final_recovery_literal_detected': bool(control and control['final_recovery_proposal_literal_detected']),
                      'earlier_private_detection_only': bool(control and control['any_earlier_private_literal_detected'] and not control['final_recovery_proposal_literal_detected']),
                      'followup_or_bound': limits.get(row['id'], row['proposed_followup']), 'owner': None, 'accepted_action': None})
assert len(remaining) == 186
(BASE / 'remaining-error-nocoverage-review.json').write_text(json.dumps({'scope': 'Original e0 Error NoCoverage only; not absence from all tests. Unverified rows remain unverified and no disposition is accepted.',
    'original_ledger_sha256': sha(original_ledger_path), 'count': len(remaining), 'rows': remaining}, indent=2) + '\n')

for number in ('04', '07'):
    directory = BASE / ('package-check-' + number)
    receipt = json.loads((directory / 'verification.json').read_text())
    jobs.append(str(Path(receipt['workspace']).parent))
    for command in receipt['commands']:
        assert command['exit_code'] == 0 and sha(directory / command['output']) == command['sha256']
package = json.loads((BASE / 'package-check-07/verification.json').read_text())
assert sha(BASE / 'private-contract-gap-proposal.patch') == package['patch_sha256']
assert sum(row['cases'] for row in json.loads((BASE / 'package-check-07/style-check.json').read_text())) == 25
final_receipt = json.loads((BASE / 'attempt-06/verification.json').read_text())
assert all(sha(BASE / name) == expected for name, expected in final_receipt['private_test_sha256'].items())
assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
final_runs = [row for row in runs if row['attempt'] == '06']
summary = {'scope': __doc__, 'source_base_commit': bindings['source_base_commit'], 'working_input_overlay': bindings['working_input_overlay'],
           'owner_unchanged': True, 'final_private_test_sha256': final_receipt['private_test_sha256'], 'patch_sha256': package['patch_sha256'],
           'proposal_cases': 25, 'final_baseline_outcomes': next(row['outcomes'] for row in final_runs if row['control'] == 'baseline'),
           'final_recovery_literal_controls': len(final_runs) - 1, 'final_recovery_literal_detections': sum(bool(row['failed_tests']) for row in final_runs if row['control'] != 'baseline'),
           'all_historical_distinct_control_ids': len(controls), 'attempts': attempts, 'test_runs': runs,
           'original_error_nocoverage_rows': 186, 'private_current_nocoverage_detections': sum(row['final_recovery_literal_detected'] for row in remaining),
           'preserved_pre_recovery_packet': str(PRIOR.relative_to(BASE.parent)), 'preserved_packet_files': len(prior_manifest),
           'completed_hosted_happy_parent_observation': {'source': original_ledger['source'], 'score': '72.36%', 'tool_exit': 2, 'threshold': 80, 'gate_unresolved': True,
                                                        'attribution': 'Parent-reported completed hosted original run; full JSON not independently available here.'},
           'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []},
           'full_mutation_run': False, 'accepted_triage_authored': False, 'gate_or_tool_changes': False, 'qualification_claim': False,
           'remaining': 'Full source-correct qualification, remaining source-supported gaps, external/default-domain boundaries and any owner disposition remain pending.'}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)}
            for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source_base_commit': bindings['source_base_commit'], 'files': manifest}, indent=2) + '\n')
print(json.dumps({'proposal_cases': 25, 'final_baseline': summary['final_baseline_outcomes'], 'final_literal_controls': summary['final_recovery_literal_controls'],
                  'final_literal_detections': summary['final_recovery_literal_detections'], 'distinct_historical_ids': len(controls), 'jobs_cleaned': len(jobs), 'owner_unchanged': True}))
